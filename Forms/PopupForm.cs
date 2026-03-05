using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clippy.Localization;
using Clippy.Models;
using Clippy.Services;

namespace Clippy.Forms
{
    public class PopupForm : Form
    {
        private readonly HistoryManager _historyManager;
        private readonly PasteService _pasteService;
        private readonly ClipboardWatcher _clipboardWatcher;

        private TextBox _searchBox = null!;
        private ListView _listView = null!;
        private Label _footerLabel = null!;
        private Label _countLabel = null!;
        private Panel _headerPanel = null!;
        private List<ClipboardEntry> _currentResults = new();

        // Image thumbnail cache
        private readonly Dictionary<string, Image> _thumbnailCache = new();

        // Colors - Dark theme
        private readonly Color _bgColor = Color.FromArgb(30, 30, 30);
        private readonly Color _headerColor = Color.FromArgb(45, 45, 48);
        private readonly Color _searchBg = Color.FromArgb(51, 51, 55);
        private readonly Color _searchFg = Color.White;
        private readonly Color _listBg = Color.FromArgb(37, 37, 38);
        private readonly Color _listFg = Color.FromArgb(220, 220, 220);
        private readonly Color _selectedBg = Color.FromArgb(0, 122, 204);
        private readonly Color _pinnedColor = Color.FromArgb(255, 185, 0);
        private readonly Color _subtitleColor = Color.FromArgb(140, 140, 140);
        private readonly Color _borderColor = Color.FromArgb(60, 60, 65);
        private readonly Color _footerBg = Color.FromArgb(40, 40, 43);
        private readonly Color _htmlBadgeColor = Color.FromArgb(86, 156, 214);
        private readonly Color _imageBadgeColor = Color.FromArgb(78, 201, 176);

        public PopupForm(HistoryManager historyManager, PasteService pasteService, ClipboardWatcher clipboardWatcher)
        {
            _historyManager = historyManager;
            _pasteService = pasteService;
            _clipboardWatcher = clipboardWatcher;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = L.Get("Popup_Title");
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(650, 500);
            BackColor = _bgColor;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            Padding = new Padding(1);

            // Header panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = _headerColor,
                Padding = new Padding(12, 10, 12, 6)
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BackColor = _searchBg,
                ForeColor = _searchFg,
                BorderStyle = BorderStyle.None,
                Text = ""
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.KeyDown += SearchBox_KeyDown;

            _countLabel = new Label
            {
                Dock = DockStyle.Right,
                Width = 70,
                Font = new Font("Segoe UI", 9f),
                ForeColor = _subtitleColor,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = _headerColor
            };

            _headerPanel.Controls.Add(_searchBox);
            _headerPanel.Controls.Add(_countLabel);

            // ListView
            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                MultiSelect = false,
                BackColor = _listBg,
                ForeColor = _listFg,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None,
                OwnerDraw = true,
                HideSelection = false
            };

            var rowHeightHack = new ImageList { ImageSize = new Size(1, 40) };
            _listView.SmallImageList = rowHeightHack;

            _listView.Columns.Add("Content", 590);
            _listView.DrawItem += ListView_DrawItem;
            _listView.DrawSubItem += ListView_DrawSubItem;
            _listView.DrawColumnHeader += ListView_DrawColumnHeader;
            _listView.MouseDoubleClick += ListView_MouseDoubleClick;
            _listView.KeyDown += ListView_KeyDown;
            _listView.Resize += (s, e) =>
            {
                if (_listView.Columns.Count > 0)
                    _listView.Columns[0].Width = _listView.ClientSize.Width;
            };

            // Footer
            _footerLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = _subtitleColor,
                BackColor = _footerBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = L.Get("Hint_Footer")
            };

            var innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _bgColor
            };

            innerPanel.Controls.Add(_listView);
            innerPanel.Controls.Add(_headerPanel);
            innerPanel.Controls.Add(_footerLabel);
            Controls.Add(innerPanel);

            Paint += (s, e) =>
            {
                using var pen = new Pen(_borderColor, 2);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        public void ShowPopup()
        {
            _searchBox.Text = "";
            RefreshList();

            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Location = new Point(
                screen.Left + (screen.Width - Width) / 2,
                screen.Top + (screen.Height - Height) / 2
            );

            Show();
            Activate();
            _searchBox.Focus();
        }

        public void HidePopup()
        {
            Hide();
        }

        private void RefreshList()
        {
            _currentResults = _historyManager.Search(_searchBox.Text);
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var entry in _currentResults)
            {
                var item = new ListViewItem(entry.Preview) { Tag = entry };
                _listView.Items.Add(item);
            }

            _listView.EndUpdate();
            _countLabel.Text = L.Get("Popup_ItemsCount", _currentResults.Count);

            if (_listView.Items.Count > 0)
            {
                _listView.Items[0].Selected = true;
                _listView.EnsureVisible(0);
            }
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e) => RefreshList();

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    e.Handled = true;
                    MoveSelection(1);
                    break;
                case Keys.Up:
                    e.Handled = true;
                    MoveSelection(-1);
                    break;
                case Keys.Enter:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    bool plainText = e.Shift; // Shift+Enter = plain text paste
                    _ = SelectCurrentItem(pasteToApp: true, plainTextOnly: plainText);
                    break;
                case Keys.Escape:
                    e.Handled = true;
                    HidePopup();
                    break;
                case Keys.Delete:
                    if (e.Control)
                    {
                        e.Handled = true;
                        DeleteCurrentItem();
                    }
                    break;
                case Keys.P:
                    if (e.Control)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        PinCurrentItem();
                    }
                    break;
            }
        }

        private void ListView_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.Handled = true;
                    bool plainText = e.Shift;
                    _ = SelectCurrentItem(pasteToApp: true, plainTextOnly: plainText);
                    break;
                case Keys.Escape:
                    e.Handled = true;
                    HidePopup();
                    break;
                case Keys.Delete:
                    e.Handled = true;
                    DeleteCurrentItem();
                    break;
                case Keys.P:
                    if (e.Control)
                    {
                        e.Handled = true;
                        PinCurrentItem();
                    }
                    break;
                case Keys.Back:
                    _searchBox.Focus();
                    break;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_listView.Items.Count == 0) return;
            int current = _listView.SelectedIndices.Count > 0 ? _listView.SelectedIndices[0] : -1;
            int next = Math.Clamp(current + delta, 0, _listView.Items.Count - 1);
            _listView.Items[next].Selected = true;
            _listView.EnsureVisible(next);
        }

        private async Task SelectCurrentItem(bool pasteToApp, bool plainTextOnly = false)
        {
            var entry = GetSelectedEntry();
            if (entry == null) return;

            HidePopup();
            _clipboardWatcher.IgnoreNext();
            await Task.Delay(100);

            if (pasteToApp)
                await _pasteService.CopyAndPaste(entry, plainTextOnly);
            else
                _pasteService.CopyToClipboard(entry, plainTextOnly);
        }

        private void DeleteCurrentItem()
        {
            var entry = GetSelectedEntry();
            if (entry == null) return;
            _historyManager.Remove(entry.Id);
            RefreshList();
        }

        private void PinCurrentItem()
        {
            var entry = GetSelectedEntry();
            if (entry == null) return;
            _historyManager.TogglePin(entry.Id);
            RefreshList();
        }

        private ClipboardEntry? GetSelectedEntry()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as ClipboardEntry;
        }

        private void ListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            _ = SelectCurrentItem(pasteToApp: true);
        }

        private void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = false;
            using var brush = new SolidBrush(_listBg);
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        private void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
        }

        private void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null) return;
            var entry = e.Item.Tag as ClipboardEntry;
            if (entry == null) return;

            var bounds = e.Bounds;
            var isSelected = e.Item.Selected;
            var g = e.Graphics;

            // Background
            using var bgBrush = new SolidBrush(isSelected ? _selectedBg : _listBg);
            g.FillRectangle(bgBrush, bounds);

            // Separator
            using var sepPen = new Pen(Color.FromArgb(50, 50, 54));
            g.DrawLine(sepPen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

            var textX = bounds.Left + 12;
            var textColor = isSelected ? Color.White : _listFg;
            var textY = bounds.Top + (bounds.Height - 18) / 2;

            // Pin icon
            if (entry.IsPinned)
            {
                var iconX = textX + 2;
                var iconY = textY + 2;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var headBrush = new SolidBrush(_pinnedColor);
                g.FillEllipse(headBrush, iconX, iconY, 10, 10);
                using var needlePen = new Pen(_pinnedColor, 2f);
                g.DrawLine(needlePen, iconX + 5, iconY + 10, iconX + 5, iconY + 16);
                g.SmoothingMode = SmoothingMode.Default;
                textX += 22;
            }

            // Type badge for HTML and Image
            if (entry.IsImage)
            {
                DrawBadge(g, ref textX, textY, "IMG", _imageBadgeColor);

                // Draw image thumbnail
                var thumb = GetThumbnail(entry.ImagePath);
                if (thumb != null)
                {
                    var thumbSize = 28;
                    var thumbY = bounds.Top + (bounds.Height - thumbSize) / 2;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(thumb, textX, thumbY, thumbSize, thumbSize);
                    textX += thumbSize + 6;
                }
            }
            else if (entry.IsHtml)
            {
                DrawBadge(g, ref textX, textY, "HTML", _htmlBadgeColor);
            }

            // Preview text
            using var contentFont = new Font("Segoe UI", 9.5f);
            using var contentBrush = new SolidBrush(textColor);
            var contentRect = new Rectangle(textX, textY, bounds.Width - textX - 90, bounds.Height - (textY - bounds.Top) - 4);
            var sf = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
                LineAlignment = StringAlignment.Near
            };
            g.DrawString(entry.Preview, contentFont, contentBrush, contentRect, sf);

            // Timestamp
            var timeText = FormatTime(entry.CreatedAt);
            using var timeFont = new Font("Segoe UI", 8f);
            using var timeBrush = new SolidBrush(isSelected ? Color.FromArgb(200, 200, 200) : _subtitleColor);
            var timeRect = new Rectangle(bounds.Right - 85, textY + 1, 80, bounds.Height - (textY - bounds.Top) - 4);
            var timeSf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(timeText, timeFont, timeBrush, timeRect, timeSf);
        }

        private void DrawBadge(Graphics g, ref int textX, int textY, string label, Color color)
        {
            using var badgeFont = new Font("Segoe UI", 7f, FontStyle.Bold);
            var badgeSize = g.MeasureString(label, badgeFont);
            var badgeRect = new Rectangle(textX, textY + 1, (int)badgeSize.Width + 8, 16);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var badgeBrush = new SolidBrush(Color.FromArgb(40, color));
            using var badgePath = CreateRoundedRect(badgeRect, 4);
            g.FillPath(badgeBrush, badgePath);

            using var borderPen = new Pen(Color.FromArgb(100, color), 1f);
            g.DrawPath(borderPen, badgePath);

            using var labelBrush = new SolidBrush(color);
            g.DrawString(label, badgeFont, labelBrush, textX + 4, textY + 2);
            g.SmoothingMode = SmoothingMode.Default;

            textX += badgeRect.Width + 6;
        }

        private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Image? GetThumbnail(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            if (_thumbnailCache.TryGetValue(imagePath, out var cached))
                return cached;

            try
            {
                using var original = Image.FromFile(imagePath);
                var thumb = original.GetThumbnailImage(56, 56, null, IntPtr.Zero);
                _thumbnailCache[imagePath] = thumb;
                return thumb;
            }
            catch { return null; }
        }

        private string FormatTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalSeconds < 60) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return dt.ToString("MMM dd");
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            HidePopup();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                HidePopup();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
