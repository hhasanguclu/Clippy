using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Clippy.Localization;
using Clippy.Models;
using Clippy.Services;

namespace Clippy.Forms;

public partial class PopupWindow : Window
{
    private readonly HistoryManager _historyManager;
    private readonly PasteService _pasteService;
    private readonly ClipboardWatcher _clipboardWatcher;
    private readonly ObservableCollection<ClipboardEntryViewModel> _items = new();

    public bool IsClosed { get; private set; }

    public PopupWindow()
    {
        InitializeComponent();

        _historyManager = App.Instance.HistoryManager;
        _pasteService = App.Instance.PasteService;
        _clipboardWatcher = App.Instance.ClipboardWatcher;

        var searchBox = this.FindControl<TextBox>("SearchBox")!;
        var historyList = this.FindControl<ListBox>("HistoryList")!;
        var footerLabel = this.FindControl<TextBlock>("FooterLabel")!;

        footerLabel.Text = L.Get("Hint_Footer");
        historyList.ItemsSource = _items;

        searchBox.TextChanged += (_, _) => RefreshList();
        searchBox.KeyDown += SearchBox_KeyDown;
        historyList.DoubleTapped += async (_, _) => await SelectCurrentItem(true);
        historyList.KeyDown += ListView_KeyDown;

        Deactivated += (_, _) => HidePopup();
        Closed += (_, _) => IsClosed = true;
    }

    public void ShowPopup()
    {
        var searchBox = this.FindControl<TextBox>("SearchBox")!;
        searchBox.Text = "";
        RefreshList();
        Show();
        Activate();
        searchBox.Focus();
    }

    public void HidePopup()
    {
        Hide();
    }

    private void RefreshList()
    {
        var searchBox = this.FindControl<TextBox>("SearchBox")!;
        var countLabel = this.FindControl<TextBlock>("CountLabel")!;
        var historyList = this.FindControl<ListBox>("HistoryList")!;

        var results = _historyManager.Search(searchBox.Text ?? "");
        _items.Clear();
        foreach (var entry in results)
            _items.Add(new ClipboardEntryViewModel(entry));

        countLabel.Text = L.Get("Popup_ItemsCount", _items.Count);

        if (_items.Count > 0)
            historyList.SelectedIndex = 0;
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                e.Handled = true;
                MoveSelection(1);
                break;
            case Key.Up:
                e.Handled = true;
                MoveSelection(-1);
                break;
            case Key.Enter:
                e.Handled = true;
                _ = SelectCurrentItem(true, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Escape:
                e.Handled = true;
                HidePopup();
                break;
            case Key.Delete:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    e.Handled = true;
                    DeleteCurrentItem();
                }
                break;
            case Key.P:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    e.Handled = true;
                    PinCurrentItem();
                }
                break;
        }
    }

    private void ListView_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                _ = SelectCurrentItem(true, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Escape:
                e.Handled = true;
                HidePopup();
                break;
            case Key.Delete:
                e.Handled = true;
                DeleteCurrentItem();
                break;
            case Key.P:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    e.Handled = true;
                    PinCurrentItem();
                }
                break;
            case Key.Back:
                this.FindControl<TextBox>("SearchBox")!.Focus();
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        var historyList = this.FindControl<ListBox>("HistoryList")!;
        if (_items.Count == 0) return;
        int current = historyList.SelectedIndex;
        int next = Math.Clamp(current + delta, 0, _items.Count - 1);
        historyList.SelectedIndex = next;
        historyList.ScrollIntoView(next);
    }

    private async Task SelectCurrentItem(bool pasteToApp, bool plainTextOnly = false)
    {
        var historyList = this.FindControl<ListBox>("HistoryList")!;
        if (historyList.SelectedItem is not ClipboardEntryViewModel vm) return;

        var entry = vm.Entry;
        HidePopup();
        _clipboardWatcher.IgnoreNext();
        await Task.Delay(100);

        if (pasteToApp)
            await _pasteService.CopyAndPaste(this, entry);
        else
            await _pasteService.CopyToClipboard(this, entry);
    }

    private void DeleteCurrentItem()
    {
        var historyList = this.FindControl<ListBox>("HistoryList")!;
        if (historyList.SelectedItem is not ClipboardEntryViewModel vm) return;
        _historyManager.Remove(vm.Entry.Id);
        RefreshList();
    }

    private void PinCurrentItem()
    {
        var historyList = this.FindControl<ListBox>("HistoryList")!;
        if (historyList.SelectedItem is not ClipboardEntryViewModel vm) return;
        _historyManager.TogglePin(vm.Entry.Id);
        RefreshList();
    }
}

public class ClipboardEntryViewModel
{
    public ClipboardEntry Entry { get; }
    private Bitmap? _thumbnail;
    private bool _thumbnailLoaded;

    public ClipboardEntryViewModel(ClipboardEntry entry)
    {
        Entry = entry;
    }

    public string Preview => Entry.Preview;
    public bool IsPinned => Entry.IsPinned;
    public bool IsImage => Entry.IsImage;
    public bool IsHtml => Entry.IsHtml;
    public string SourceApp => Entry.SourceApp;
    public bool HasSourceApp => !string.IsNullOrEmpty(Entry.SourceApp);

    public Bitmap? Thumbnail
    {
        get
        {
            if (!_thumbnailLoaded)
            {
                _thumbnailLoaded = true;
                _thumbnail = LoadThumbnail();
            }
            return _thumbnail;
        }
    }

    private Bitmap? LoadThumbnail()
    {
        if (!Entry.IsImage || string.IsNullOrEmpty(Entry.ImagePath))
            return null;

        try
        {
            if (!File.Exists(Entry.ImagePath))
                return null;

            using var stream = File.OpenRead(Entry.ImagePath);
            return Bitmap.DecodeToWidth(stream, 88);
        }
        catch
        {
            return null;
        }
    }

    public string TimeText
    {
        get
        {
            var diff = DateTime.Now - Entry.CreatedAt;
            if (diff.TotalSeconds < 60) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return Entry.CreatedAt.ToString("MMM dd");
        }
    }
}
