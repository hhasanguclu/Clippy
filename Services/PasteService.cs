using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clippy.Models;

namespace Clippy.Services
{
    public class PasteService
    {
        private IntPtr _previousForegroundWindow;

        public void RememberForegroundWindow()
        {
            _previousForegroundWindow = NativeMethods.GetForegroundWindow();
        }

        /// <summary>
        /// Places the entry content back onto the clipboard in its original format.
        /// </summary>
        public void CopyToClipboard(ClipboardEntry entry, bool plainTextOnly = false)
        {
            switch (entry.EntryType)
            {
                case ClipboardEntryType.Image:
                    if (entry.ImagePath != null && File.Exists(entry.ImagePath))
                    {
                        using var img = Image.FromFile(entry.ImagePath);
                        Clipboard.SetImage(img);
                    }
                    break;

                case ClipboardEntryType.Html:
                    if (plainTextOnly || string.IsNullOrEmpty(entry.HtmlContent))
                    {
                        Clipboard.SetText(entry.Content);
                    }
                    else
                    {
                        var dataObj = new DataObject();
                        dataObj.SetData(DataFormats.Html, entry.HtmlContent);
                        dataObj.SetData(DataFormats.UnicodeText, entry.Content);
                        Clipboard.SetDataObject(dataObj, true);
                    }
                    break;

                default: // Text
                    if (!string.IsNullOrEmpty(entry.Content))
                        Clipboard.SetText(entry.Content);
                    break;
            }
        }

        /// <summary>
        /// Copies entry to clipboard, activates previous window, sends Ctrl+V.
        /// </summary>
        public async Task CopyAndPaste(ClipboardEntry entry, bool plainTextOnly = false)
        {
            CopyToClipboard(entry, plainTextOnly);

            if (_previousForegroundWindow != IntPtr.Zero)
            {
                await Task.Delay(50);
                ActivateWindow(_previousForegroundWindow);
                await Task.Delay(50);
                SendKeys.SendWait("^v");
            }
        }

        private void ActivateWindow(IntPtr hwnd)
        {
            var currentThread = NativeMethods.GetCurrentThreadId();
            var targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);

            if (currentThread != targetThread)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, true);
                NativeMethods.SetForegroundWindow(hwnd);
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            }
            else
            {
                NativeMethods.SetForegroundWindow(hwnd);
            }
        }
    }
}
