using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Clippy.Assets;
using Clippy.Localization;
using Clippy.Services;

namespace Clippy;

public partial class App : Application
{
    private ClipboardWatcher? _clipboardWatcher;
    private HistoryManager? _historyManager;
    private HotkeyManager? _hotkeyManager;
    private PasteService? _pasteService;
    private DatabaseService? _databaseService;
    private AppSettings _settings = new();
    private string _settingsPath = string.Empty;
    private NativeMenu? _trayMenu;
    private TrayIcon? _trayIcon;
    private Forms.PopupWindow? _popupWindow;

    public static App Instance => (App)Current!;
    public HistoryManager HistoryManager => _historyManager!;
    public PasteService PasteService => _pasteService!;
    public ClipboardWatcher ClipboardWatcher => _clipboardWatcher!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load settings
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Clippy", "settings.json");
            _settings = LoadSettings();
            L.SetLanguage(_settings.Language);

            // Initialize services
            _databaseService = new DatabaseService();
            _historyManager = new HistoryManager(_databaseService) { MaxEntries = _settings.MaxHistoryItems };
            _clipboardWatcher = new ClipboardWatcher(_databaseService.ImagesDirectory);
            _hotkeyManager = new HotkeyManager();
            _pasteService = new PasteService();

            _clipboardWatcher.ClipboardChanged += entry => _historyManager.Add(entry);
            _clipboardWatcher.Start();

            // Setup tray icon
            SetupTrayIcon();

            // Register hotkey
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.Register();

            // Don't show a main window - tray app
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.ShutdownRequested += (_, _) => Cleanup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon()
    {
        _trayMenu = new NativeMenu();

        var showItem = new NativeMenuItem(L.Get("TrayMenu_ShowHistory"));
        showItem.Click += (_, _) => ShowPopup();
        _trayMenu.Items.Add(showItem);

        _trayMenu.Items.Add(new NativeMenuItemSeparator());

        var pauseItem = new NativeMenuItem(L.Get("TrayMenu_PauseCapture"));
        pauseItem.Click += (_, _) =>
        {
            _clipboardWatcher!.IsPaused = !_clipboardWatcher.IsPaused;
            pauseItem.Header = _clipboardWatcher.IsPaused
                ? L.Get("TrayMenu_ResumeCapture")
                : L.Get("TrayMenu_PauseCapture");
        };
        _trayMenu.Items.Add(pauseItem);

        var ignoreItem = new NativeMenuItem(L.Get("TrayMenu_IgnoreNext"));
        ignoreItem.Click += (_, _) => _clipboardWatcher!.IgnoreNext();
        _trayMenu.Items.Add(ignoreItem);

        _trayMenu.Items.Add(new NativeMenuItemSeparator());

        var clearItem = new NativeMenuItem(L.Get("TrayMenu_ClearHistory"));
        clearItem.Click += (_, _) => _historyManager!.Clear(keepPinned: true);
        _trayMenu.Items.Add(clearItem);

        _trayMenu.Items.Add(new NativeMenuItemSeparator());

        var settingsItem = new NativeMenuItem(L.Get("TrayMenu_Settings"));
        settingsItem.Click += (_, _) => ShowSettings();
        _trayMenu.Items.Add(settingsItem);

        var exitItem = new NativeMenuItem(L.Get("TrayMenu_Exit"));
        exitItem.Click += (_, _) => ExitApplication();
        _trayMenu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = TrayIconGenerator.CreateClipboardIcon(),
            ToolTipText = L.Get("Tooltip_TrayIcon"),
            Menu = _trayMenu,
            IsVisible = true
        };
        _trayIcon.Clicked += (_, _) => ShowPopup();

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private void OnHotkeyPressed()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_popupWindow != null && _popupWindow.IsVisible)
                _popupWindow.HidePopup();
            else
                ShowPopup();
        });
    }

    public void ShowPopup()
    {
        if (_popupWindow == null || _popupWindow.IsClosed)
            _popupWindow = new Forms.PopupWindow();

        _popupWindow.ShowPopup();
    }

    private void ShowSettings()
    {
        var settingsWindow = new Forms.SettingsWindow(_settings.Language, _settings.MaxHistoryItems);
        settingsWindow.SettingsSaved += (lang, maxItems, autoStart) =>
        {
            bool languageChanged = _settings.Language != lang;
            _settings.Language = lang;
            _settings.MaxHistoryItems = maxItems;
            _historyManager!.MaxEntries = maxItems;
            SaveSettings();

            StartupService.SetStartup(autoStart);

            if (languageChanged)
            {
                L.SetLanguage(lang);
                // Rebuild tray menu
                SetupTrayIcon();
                _popupWindow?.Close();
                _popupWindow = null;
            }
        };
        settingsWindow.Show();
    }

    private void ExitApplication()
    {
        Cleanup();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void Cleanup()
    {
        _trayIcon?.Dispose();
        _clipboardWatcher?.Dispose();
        _hotkeyManager?.Dispose();
        _databaseService?.Dispose();
        _popupWindow?.Close();
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
