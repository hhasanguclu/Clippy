using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Clippy.Models;

namespace Clippy.Services
{
    public class ClipboardWatcher : IDisposable
    {
        private readonly ClipboardListenerForm _listenerForm;
        private string _lastHash = string.Empty;
        private bool _isPaused;
        private bool _ignoreNext;
        private readonly string _imagesDir;

        public event Action<ClipboardEntry>? ClipboardChanged;

        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        public ClipboardWatcher(string imagesDir)
        {
            _imagesDir = imagesDir;
            Directory.CreateDirectory(_imagesDir);
            _listenerForm = new ClipboardListenerForm(this);
        }

        public void IgnoreNext()
        {
            _ignoreNext = true;
        }

        internal void OnClipboardUpdate()
        {
            if (_isPaused) return;

            if (_ignoreNext)
            {
                _ignoreNext = false;
                return;
            }

            try
            {
                string sourceApp = GetSourceApp();

                // Priority: Image > HTML > Text
                if (Clipboard.ContainsImage())
                {
                    HandleImageClipboard(sourceApp);
                }
                else if (Clipboard.ContainsText(TextDataFormat.Html))
                {
                    HandleHtmlClipboard(sourceApp);
                }
                else if (Clipboard.ContainsText())
                {
                    HandleTextClipboard(sourceApp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Clipboard read error: {ex.Message}");
            }
        }

        private void HandleTextClipboard(string sourceApp)
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var hash = ClipboardEntry.ComputeHash(text);
            if (hash == _lastHash) return;
            _lastHash = hash;

            var entry = new ClipboardEntry
            {
                Content = text,
                Preview = ClipboardEntry.CreatePreview(text),
                ContentHash = hash,
                SourceApp = sourceApp,
                EntryType = ClipboardEntryType.Text,
                CreatedAt = DateTime.Now
            };

            ClipboardChanged?.Invoke(entry);
        }

        private void HandleHtmlClipboard(string sourceApp)
        {
            var html = Clipboard.GetText(TextDataFormat.Html);
            var plainText = Clipboard.ContainsText(TextDataFormat.UnicodeText)
                ? Clipboard.GetText(TextDataFormat.UnicodeText)
                : Clipboard.GetText();

            if (string.IsNullOrWhiteSpace(plainText) && string.IsNullOrWhiteSpace(html)) return;

            var hashSource = !string.IsNullOrWhiteSpace(plainText) ? plainText : html;
            var hash = ClipboardEntry.ComputeHash(hashSource);
            if (hash == _lastHash) return;
            _lastHash = hash;

            var entry = new ClipboardEntry
            {
                Content = plainText ?? string.Empty,
                HtmlContent = html,
                Preview = ClipboardEntry.CreatePreview(plainText ?? "[HTML Content]"),
                ContentHash = hash,
                SourceApp = sourceApp,
                EntryType = ClipboardEntryType.Html,
                CreatedAt = DateTime.Now
            };

            ClipboardChanged?.Invoke(entry);
        }

        private void HandleImageClipboard(string sourceApp)
        {
            var image = Clipboard.GetImage();
            if (image == null) return;

            // Save image to disk
            var fileName = $"clip_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            var imagePath = Path.Combine(_imagesDir, fileName);

            using (var bmp = new Bitmap(image))
            {
                bmp.Save(imagePath, ImageFormat.Png);
            }

            // Compute hash from file bytes
            var fileBytes = File.ReadAllBytes(imagePath);
            var hash = ClipboardEntry.ComputeHash(fileBytes);
            if (hash == _lastHash)
            {
                // Duplicate — delete saved file
                try { File.Delete(imagePath); } catch { }
                return;
            }
            _lastHash = hash;

            var entry = new ClipboardEntry
            {
                Content = $"[Image {image.Width}x{image.Height}]",
                Preview = $"🖼 Image ({image.Width}×{image.Height})",
                ImagePath = imagePath,
                ContentHash = hash,
                SourceApp = sourceApp,
                EntryType = ClipboardEntryType.Image,
                CreatedAt = DateTime.Now
            };

            image.Dispose();
            ClipboardChanged?.Invoke(entry);
        }

        private string GetSourceApp()
        {
            try
            {
                var foreground = NativeMethods.GetForegroundWindow();
                NativeMethods.GetWindowThreadProcessId(foreground, out uint pid);
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName;
            }
            catch { return string.Empty; }
        }

        public void Dispose()
        {
            _listenerForm?.Dispose();
        }

        private class ClipboardListenerForm : Form
        {
            private readonly ClipboardWatcher _watcher;

            public ClipboardListenerForm(ClipboardWatcher watcher)
            {
                _watcher = watcher;
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
                Size = new System.Drawing.Size(0, 0);
                Opacity = 0;
                Show();
                Hide();
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    _watcher.OnClipboardUpdate();
                }
                base.WndProc(ref m);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    NativeMethods.RemoveClipboardFormatListener(Handle);
                base.Dispose(disposing);
            }
        }
    }
}
