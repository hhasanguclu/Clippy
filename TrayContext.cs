using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Clippy.Forms;
using Clippy.Localization;
using Clippy.Services;

namespace Clippy
{
    public class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ClipboardWatcher _clipboardWatcher;
        private readonly HistoryManager _historyManager;
        private readonly HotkeyManager _hotkeyManager;
        private readonly PasteService _pasteService;
        private readonly DatabaseService _databaseService;
        private PopupForm? _popupForm;
        private ToolStripMenuItem _pauseMenuItem = null!;
        private AppSettings _settings;
        private readonly string _settingsPath;

        public TrayContext()
        {
            // Load settings
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Clippy", "settings.json");
            _settings = LoadSettings();

            // Apply language
            L.SetLanguage(_settings.Language);

            // Initialize services
            _databaseService = new DatabaseService();
            _historyManager = new HistoryManager(_databaseService) { MaxEntries = _settings.MaxHistoryItems };
            _clipboardWatcher = new ClipboardWatcher(_databaseService.ImagesDirectory);
            _hotkeyManager = new HotkeyManager();
            _pasteService = new PasteService();

            // Clipboard events
            _clipboardWatcher.ClipboardChanged += OnClipboardChanged;

            // Hotkey
            _trayIcon = CreateTrayIcon();
            if (!_hotkeyManager.Register())
            {
                ShowBalloon(L.Get("Msg_HotkeyFailed"), ToolTipIcon.Warning);
            }
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        }

        private NotifyIcon CreateTrayIcon()
        {
            var icon = CreateClipboardIcon();
            var trayIcon = new NotifyIcon
            {
                Icon = icon,
                Text = L.Get("Tooltip_TrayIcon"),
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };
            trayIcon.DoubleClick += (s, e) => ShowPopup();
            return trayIcon;
        }

        private Icon CreateClipboardIcon()
        {
            var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var bodyBrush = new SolidBrush(Color.FromArgb(0, 122, 204));
            g.FillRoundedRectangle(bodyBrush, 4, 6, 24, 22, 3);

            using var clipPen = new Pen(Color.FromArgb(0, 90, 158), 2.5f);
            g.DrawRectangle(clipPen, 10, 2, 12, 8);

            using var linePen = new Pen(Color.White, 1.5f);
            g.DrawLine(linePen, 9, 15, 23, 15);
            g.DrawLine(linePen, 9, 19, 23, 19);
            g.DrawLine(linePen, 9, 23, 18, 23);

            return Icon.FromHandle(bmp.GetHicon());
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Font = new Font("Segoe UI", 9.5f);
            menu.Renderer = new DarkMenuRenderer();

            menu.Items.Add(L.Get("TrayMenu_ShowHistory"), null, (s, e) => ShowPopup());
            menu.Items.Add(new ToolStripSeparator());

            _pauseMenuItem = new ToolStripMenuItem(L.Get("TrayMenu_PauseCapture"));
            _pauseMenuItem.Click += (s, e) => TogglePause();
            menu.Items.Add(_pauseMenuItem);

            menu.Items.Add(L.Get("TrayMenu_IgnoreNext"), null, (s, e) =>
            {
                _clipboardWatcher.IgnoreNext();
                ShowBalloon(L.Get("Msg_IgnoreNextActive"), ToolTipIcon.Info);
            });

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L.Get("TrayMenu_ClearHistory"), null, (s, e) =>
            {
                _historyManager.Clear(keepPinned: true);
                ShowBalloon(L.Get("Msg_HistoryCleared"), ToolTipIcon.Info);
            });

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L.Get("TrayMenu_Settings"), null, (s, e) => ShowSettings());
            menu.Items.Add(L.Get("TrayMenu_Exit"), null, (s, e) => ExitApplication());

            return menu;
        }

        private void OnClipboardChanged(Models.ClipboardEntry entry)
        {
            _historyManager.Add(entry);
        }

        private void OnHotkeyPressed()
        {
            if (_popupForm != null && _popupForm.Visible)
                _popupForm.HidePopup();
            else
                ShowPopup();
        }

        private void ShowPopup()
        {
            _pasteService.RememberForegroundWindow();

            if (_popupForm == null || _popupForm.IsDisposed)
                _popupForm = new PopupForm(_historyManager, _pasteService, _clipboardWatcher);

            _popupForm.ShowPopup();
        }

        private void TogglePause()
        {
            _clipboardWatcher.IsPaused = !_clipboardWatcher.IsPaused;
            if (_clipboardWatcher.IsPaused)
            {
                _pauseMenuItem.Text = L.Get("TrayMenu_ResumeCapture");
                ShowBalloon(L.Get("Msg_CapturePaused"), ToolTipIcon.Info);
            }
            else
            {
                _pauseMenuItem.Text = L.Get("TrayMenu_PauseCapture");
                ShowBalloon(L.Get("Msg_CaptureResumed"), ToolTipIcon.Info);
            }
        }

        private void ShowSettings()
        {
            using var form = new SettingsForm(_settings.Language, _settings.MaxHistoryItems);
            if (form.ShowDialog() == DialogResult.OK)
            {
                bool languageChanged = _settings.Language != form.SelectedLanguage;
                _settings.Language = form.SelectedLanguage;
                _settings.MaxHistoryItems = form.MaxHistoryItems;
                _historyManager.MaxEntries = _settings.MaxHistoryItems;
                SaveSettings();

                // Handle startup setting
                StartupService.SetStartup(form.StartWithWindows);

                if (languageChanged)
                {
                    L.SetLanguage(_settings.Language);
                    _trayIcon.ContextMenuStrip = CreateContextMenu();
                    _trayIcon.Text = L.Get("Tooltip_TrayIcon");
                    _popupForm?.Dispose();
                    _popupForm = null;
                    ShowBalloon(L.Get("Settings_RestartRequired"), ToolTipIcon.Info);
                }
            }
        }

        private void ShowBalloon(string text, ToolTipIcon icon)
        {
            _trayIcon.BalloonTipTitle = "Clippy";
            _trayIcon.BalloonTipText = text;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(2000);
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            _clipboardWatcher.Dispose();
            _hotkeyManager.Dispose();
            _databaseService.Dispose();
            _popupForm?.Dispose();
            Application.Exit();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath)!;
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }
    }

    public class AppSettings
    {
        public string Language { get; set; } = "en";
        public int MaxHistoryItems { get; set; } = 200;
    }

    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(220, 220, 220);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using var brush = new SolidBrush(e.Item.Selected
                ? Color.FromArgb(62, 62, 66)
                : Color.FromArgb(45, 45, 48));
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }
    }

    internal class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Color.FromArgb(60, 60, 65);
        public override Color MenuItemBorder => Color.FromArgb(62, 62, 66);
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color MenuStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(62, 62, 66);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(62, 62, 66);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(50, 50, 53);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(50, 50, 53);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color SeparatorDark => Color.FromArgb(60, 60, 65);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 65);
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
