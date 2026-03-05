using System;
using System.Drawing;
using System.Windows.Forms;
using Clippy.Localization;
using Clippy.Services;

namespace Clippy.Forms
{
    public class SettingsForm : Form
    {
        private ComboBox _languageCombo = null!;
        private NumericUpDown _maxHistoryNumeric = null!;
        private CheckBox _startupCheckBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        // Colors - Dark theme
        private readonly Color _bgColor = Color.FromArgb(30, 30, 30);
        private readonly Color _controlBg = Color.FromArgb(51, 51, 55);
        private readonly Color _fg = Color.FromArgb(220, 220, 220);
        private readonly Color _accentColor = Color.FromArgb(0, 122, 204);

        public string SelectedLanguage { get; private set; } = "en";
        public int MaxHistoryItems { get; private set; } = 200;
        public bool StartWithWindows { get; private set; }

        public SettingsForm(string currentLanguage, int currentMaxHistory)
        {
            SelectedLanguage = currentLanguage;
            MaxHistoryItems = currentMaxHistory;
            StartWithWindows = StartupService.IsStartupEnabled();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = L.Get("Settings_Title");
            Size = new Size(420, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = _bgColor;
            ForeColor = _fg;
            Font = new Font("Segoe UI", 10f);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(20, 20, 20, 10)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Language
            var lblLang = new Label
            {
                Text = L.Get("Settings_Language"),
                ForeColor = _fg,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 0)
            };

            _languageCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = _controlBg,
                ForeColor = _fg,
                FlatStyle = FlatStyle.Flat
            };
            _languageCombo.Items.AddRange(new object[] { "English", "Türkçe" });
            _languageCombo.SelectedIndex = SelectedLanguage == "tr" ? 1 : 0;

            // Max history
            var lblMax = new Label
            {
                Text = L.Get("Settings_MaxHistory"),
                ForeColor = _fg,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 0)
            };

            _maxHistoryNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 10,
                Maximum = 10000,
                Value = MaxHistoryItems,
                BackColor = _controlBg,
                ForeColor = _fg,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Start with Windows
            _startupCheckBox = new CheckBox
            {
                Text = L.Get("Settings_StartWithWindows"),
                ForeColor = _fg,
                AutoSize = true,
                Checked = StartWithWindows,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 0)
            };

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0)
            };

            _cancelButton = new Button
            {
                Text = L.Get("Settings_Cancel"),
                Size = new Size(90, 34),
                BackColor = _controlBg,
                ForeColor = _fg,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(5, 5, 0, 5)
            };
            _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            _saveButton = new Button
            {
                Text = L.Get("Settings_Save"),
                Size = new Size(90, 34),
                BackColor = _accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Margin = new Padding(5)
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            mainPanel.Controls.Add(lblLang, 0, 0);
            mainPanel.Controls.Add(_languageCombo, 1, 0);
            mainPanel.Controls.Add(lblMax, 0, 1);
            mainPanel.Controls.Add(_maxHistoryNumeric, 1, 1);
            mainPanel.SetColumnSpan(_startupCheckBox, 2);
            mainPanel.Controls.Add(_startupCheckBox, 0, 2);
            mainPanel.SetColumnSpan(buttonPanel, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 4);

            Controls.Add(mainPanel);
            AcceptButton = _saveButton;
            CancelButton = _cancelButton;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            SelectedLanguage = _languageCombo.SelectedIndex == 1 ? "tr" : "en";
            MaxHistoryItems = (int)_maxHistoryNumeric.Value;
            StartWithWindows = _startupCheckBox.Checked;
        }
    }
}
