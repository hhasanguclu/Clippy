using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clippy.Localization;
using Clippy.Services;

namespace Clippy.Forms;

public partial class SettingsWindow : Window
{
    public event Action<string, int, bool>? SettingsSaved;

    // Required for XAML loader
    public SettingsWindow() : this("en", 200) { }

    public SettingsWindow(string currentLanguage, int currentMaxHistory)
    {
        InitializeComponent();

        var languageCombo = this.FindControl<ComboBox>("LanguageCombo")!;
        var maxHistoryNumeric = this.FindControl<NumericUpDown>("MaxHistoryNumeric")!;
        var startupCheckBox = this.FindControl<CheckBox>("StartupCheckBox")!;
        var saveButton = this.FindControl<Button>("SaveButton")!;
        var cancelButton = this.FindControl<Button>("CancelButton")!;

        // Apply localized labels
        Title = L.Get("Settings_Title");
        saveButton.Content = L.Get("Settings_Save");
        cancelButton.Content = L.Get("Settings_Cancel");

        // Set current values
        languageCombo.SelectedIndex = currentLanguage == "tr" ? 1 : 0;
        maxHistoryNumeric.Value = currentMaxHistory;
        startupCheckBox.IsChecked = StartupService.IsStartupEnabled();

        saveButton.Click += (_, _) =>
        {
            var lang = languageCombo.SelectedIndex == 1 ? "tr" : "en";
            var maxItems = (int)(maxHistoryNumeric.Value ?? 200);
            var autoStart = startupCheckBox.IsChecked ?? false;
            SettingsSaved?.Invoke(lang, maxItems, autoStart);
            Close();
        };

        cancelButton.Click += (_, _) => Close();
    }
}
