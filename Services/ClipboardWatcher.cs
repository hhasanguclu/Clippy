using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Clippy.Models;

namespace Clippy.Services;

public class ClipboardWatcher : IDisposable
{
    private string _lastHash = string.Empty;
    private bool _isPaused;
    private bool _ignoreNext;
    private readonly string _imagesDir;
    private Timer? _pollTimer;
    private Window? _hiddenWindow;

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
    }

    /// <summary>
    /// Must be called from the UI thread after the application is initialized.
    /// Creates a hidden window for clipboard access and starts polling.
    /// </summary>
    public void Start()
    {
        _hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            Opacity = 0,
            IsVisible = false
        };
        // Show then immediately hide to initialize the platform handle
        _hiddenWindow.Show();
        _hiddenWindow.Hide();

        _pollTimer = new Timer(_ => PollClipboard(), null, 500, 500);
    }

    public void IgnoreNext()
    {
        _ignoreNext = true;
    }

    private void PollClipboard()
    {
        if (_isPaused) return;

        try
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await CheckClipboardAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Clipboard poll error: {ex.Message}");
                }
            });
        }
        catch { }
    }

    private async Task CheckClipboardAsync()
    {
        if (_isPaused || _hiddenWindow == null) return;

        var clipboard = TopLevel.GetTopLevel(_hiddenWindow)?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;

        var hash = ClipboardEntry.ComputeHash(text);
        if (hash == _lastHash) return;

        if (_ignoreNext)
        {
            _ignoreNext = false;
            _lastHash = hash;
            return;
        }

        _lastHash = hash;

        var entry = new ClipboardEntry
        {
            Content = text,
            Preview = ClipboardEntry.CreatePreview(text),
            ContentHash = hash,
            SourceApp = string.Empty,
            EntryType = ClipboardEntryType.Text,
            CreatedAt = DateTime.Now
        };

        ClipboardChanged?.Invoke(entry);
    }

    public void UpdateLastHash(string hash)
    {
        _lastHash = hash;
    }

    public void Dispose()
    {
        _pollTimer?.Dispose();
        _hiddenWindow?.Close();
    }
}
