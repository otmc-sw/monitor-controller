using monitor_controller.Configuration;
using monitor_controller.Display;
using monitor_controller.Scheduling;

namespace monitor_controller.UI;

public sealed class SettingsForm : Form
{
    private readonly IDisplayController _displayController;
    private readonly DisplayScheduler _scheduler;
    private readonly ConfigService _configService;
    private AppConfig _config;
    private readonly PhysicalMonitorInfo[] _monitors;
    private readonly DisplayProfile? _currentProfile;

    private readonly ListBox _profileList;
    private readonly TextBox _timeTextBox;
    private readonly NumericUpDown _brightnessNumeric;
    private readonly NumericUpDown _contrastNumeric;
    private readonly ComboBox _monitorComboBox;
    private readonly Label _statusLabel;
    private readonly Button _addButton;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private readonly Button _saveButton;
    private readonly Button _applyButton;

    // Manual test controls
    private readonly Button _setBrightness50Button;
    private readonly Button _setContrast50Button;
    private readonly Label _readBrightnessLabel;
    private readonly Label _readContrastLabel;

    public SettingsForm(
        IDisplayController displayController,
        DisplayScheduler scheduler,
        ConfigService configService,
        AppConfig config,
        PhysicalMonitorInfo[] monitors,
        DisplayProfile? currentProfile)
    {
        _displayController = displayController;
        _scheduler = scheduler;
        _configService = configService;
        _config = config;
        _monitors = monitors;
        _currentProfile = currentProfile;

        Text = "Monitor Controller Settings";
        Width = 520;
        Height = 560;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(12),
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Monitor selection
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Profiles label
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Profile list
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Time
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Brightness
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Contrast
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Add/Edit/Delete
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Manual test label
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Manual test buttons
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status/Save
        Controls.Add(layout);

        // Monitor selection
        layout.Controls.Add(new Label { Text = "Monitor:", Anchor = AnchorStyles.Left }, 0, 0);
        _monitorComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        layout.Controls.Add(_monitorComboBox, 1, 0);

        // Profiles label
        layout.Controls.Add(new Label { Text = "Profiles:", Anchor = AnchorStyles.Left }, 0, 1);

        // Profile list (expands to fill available space)
        _profileList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        layout.Controls.Add(_profileList, 1, 2);

        // Time
        layout.Controls.Add(new Label { Text = "Time (HH:mm):", Anchor = AnchorStyles.Left }, 0, 3);
        _timeTextBox = new TextBox { Dock = DockStyle.Fill, Text = "08:00" };
        layout.Controls.Add(_timeTextBox, 1, 3);

        // Brightness
        layout.Controls.Add(new Label { Text = "Brightness (0-100):", Anchor = AnchorStyles.Left }, 0, 4);
        _brightnessNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 50 };
        layout.Controls.Add(_brightnessNumeric, 1, 4);

        // Contrast
        layout.Controls.Add(new Label { Text = "Contrast (0-100):", Anchor = AnchorStyles.Left }, 0, 5);
        _contrastNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 50 };
        layout.Controls.Add(_contrastNumeric, 1, 5);

        // Buttons row
        var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _addButton = new Button { Text = "Add", Width = 80 };
        _editButton = new Button { Text = "Edit", Width = 80 };
        _deleteButton = new Button { Text = "Delete", Width = 80 };
        _applyButton = new Button { Text = "Apply", Width = 80 };
        buttonsPanel.Controls.AddRange(new Control[] { _addButton, _editButton, _deleteButton, _applyButton });
        layout.Controls.Add(buttonsPanel, 1, 6);

        // Manual test section
        layout.Controls.Add(new Label { Text = "Manual Test (DDC/CI):", Anchor = AnchorStyles.Left }, 0, 7);

        var testPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _setBrightness50Button = new Button { Text = "Set Brightness 50", Width = 130 };
        _setContrast50Button = new Button { Text = "Set Contrast 50", Width = 120 };
        var readValuesButton = new Button { Text = "Read Values", Width = 100 };
        testPanel.Controls.AddRange(new Control[]
        {
            _setBrightness50Button,
            _setContrast50Button,
            readValuesButton
        });
        layout.Controls.Add(testPanel, 1, 7);
        layout.SetColumnSpan(testPanel, 1);

        // Read results
        layout.Controls.Add(new Label { Text = "Current monitor values:", Anchor = AnchorStyles.Left }, 0, 8);
        var readPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _readBrightnessLabel = new Label { Text = "Brightness: --", AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
        _readContrastLabel = new Label { Text = "Contrast: --", AutoSize = true };
        readPanel.Controls.AddRange(new Control[] { _readBrightnessLabel, _readContrastLabel });
        layout.Controls.Add(readPanel, 1, 8);

        // Status and Save
        layout.Controls.Add(new Label { Text = "", Anchor = AnchorStyles.Left }, 0, 9);
        var statusPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };
        _statusLabel = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 10, 0)
        };
        _saveButton = new Button { Text = "Save", Width = 90 };
        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(_saveButton);
        layout.Controls.Add(statusPanel, 1, 9);

        // Wire up events
        _profileList.SelectedIndexChanged += OnProfileSelected;
        _addButton.Click += OnAddProfile;
        _editButton.Click += OnEditProfile;
        _deleteButton.Click += OnDeleteProfile;
        _applyButton.Click += OnApplyProfile;
        _saveButton.Click += OnSave;
        _setBrightness50Button.Click += async (s, e) => await SetBrightnessTestAsync(50);
        _setContrast50Button.Click += async (s, e) => await SetContrastTestAsync(50);
        readValuesButton.Click += async (s, e) => await ReadValuesAsync();

        LoadConfig();
    }

    private void LoadConfig()
    {
        // Populate monitor combo
        _monitorComboBox.Items.Clear();
        foreach (var monitor in _monitors)
        {
            _monitorComboBox.Items.Add(monitor);
        }

        if (_monitors.Length > 0)
        {
            // Select the configured monitor if found, otherwise first
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(_config.SelectedMonitorHandle))
            {
                try
                {
                    var savedHandle = new IntPtr(long.Parse(_config.SelectedMonitorHandle));
                    for (int i = 0; i < _monitors.Length; i++)
                    {
                        if (_monitors[i].Handle == savedHandle)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }
                catch
                {
                    // Invalid saved handle - fall back to first monitor
                }
            }
            _monitorComboBox.SelectedIndex = selectedIndex;
        }

        // Populate profile list
        RefreshProfileList();
    }

    private void RefreshProfileList()
    {
        _profileList.Items.Clear();
        foreach (var profile in _config.Profiles.OrderBy(p => p.TimeOnly))
        {
            bool isActive = _currentProfile != null &&
                            _currentProfile.Time == profile.Time &&
                            _currentProfile.Brightness == profile.Brightness &&
                            _currentProfile.Contrast == profile.Contrast;
            string marker = isActive ? " [ACTIVE]" : "";
            _profileList.Items.Add($"{profile.Time}  B:{profile.Brightness}  C:{profile.Contrast}{marker}");
        }

        // Enable/disable edit/delete buttons
        bool hasSelection = _profileList.SelectedIndex >= 0;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private void OnProfileSelected(object? sender, EventArgs e)
    {
        int index = _profileList.SelectedIndex;
        if (index < 0) return;

        var profile = _config.Profiles.OrderBy(p => p.TimeOnly).ElementAt(index);
        _timeTextBox.Text = profile.Time;
        _brightnessNumeric.Value = profile.Brightness;
        _contrastNumeric.Value = profile.Contrast;

        _editButton.Enabled = true;
        _deleteButton.Enabled = true;
    }

    private void OnAddProfile(object? sender, EventArgs e)
    {
        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast))
        {
            return;
        }

        // Check for duplicate time
        if (_config.Profiles.Any(p => p.Time == time))
        {
            _statusLabel.Text = "A profile with this time already exists.";
            _statusLabel.ForeColor = Color.OrangeRed;
            return;
        }

        _config.Profiles.Add(new DisplayProfile(time, brightness, contrast));
        _statusLabel.Text = $"Profile added: {time}";
        _statusLabel.ForeColor = Color.ForestGreen;
        RefreshProfileList();
    }

    private void OnEditProfile(object? sender, EventArgs e)
    {
        var profiles = _config.Profiles.OrderBy(p => p.TimeOnly).ToArray();
        int index = _profileList.SelectedIndex;
        if (index < 0 || index >= profiles.Length) return;

        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast))
        {
            return;
        }

        // Check for duplicate time (excluding the one being edited)
        if (_config.Profiles.Any(p => p.Time == time && p.Time != profiles[index].Time))
        {
            _statusLabel.Text = "A profile with this time already exists.";
            _statusLabel.ForeColor = Color.OrangeRed;
            return;
        }

        _config.Profiles.Remove(profiles[index]);
        _config.Profiles.Add(new DisplayProfile(time, brightness, contrast));
        _statusLabel.Text = $"Profile edited: {time}";
        _statusLabel.ForeColor = Color.ForestGreen;
        RefreshProfileList();
    }

    private void OnDeleteProfile(object? sender, EventArgs e)
    {
        var profiles = _config.Profiles.OrderBy(p => p.TimeOnly).ToArray();
        int index = _profileList.SelectedIndex;
        if (index < 0 || index >= profiles.Length) return;

        var toDelete = profiles[index];
        if (MessageBox.Show(
                $"Delete profile '{toDelete.Time}' (B:{toDelete.Brightness} C:{toDelete.Contrast})?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _config.Profiles.Remove(toDelete);
            _statusLabel.Text = $"Profile deleted: {toDelete.Time}";
            _statusLabel.ForeColor = Color.ForestGreen;
            RefreshProfileList();
        }
    }

    private async void OnApplyProfile(object? sender, EventArgs e)
    {
        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast))
        {
            return;
        }

        var profile = new DisplayProfile(time, brightness, contrast);
        await _scheduler.ApplyProfileAsync(profile);
        _statusLabel.Text = $"Applied profile: {time} (B:{brightness} C:{contrast})";
        _statusLabel.ForeColor = Color.ForestGreen;
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        string? selectedMonitorHandle = null;
        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo selectedMonitor)
        {
            selectedMonitorHandle = selectedMonitor.Handle.ToString();
        }

        _config = _config with { SelectedMonitorHandle = selectedMonitorHandle };
        await _configService.SaveAsync(_config);

        // Update scheduler with the monitor selection
        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            _scheduler.SetSelectedMonitor(monitor.Handle);
        }

        _statusLabel.Text = "Configuration saved.";
        _statusLabel.ForeColor = Color.ForestGreen;
    }

    private bool TryParseCurrentValues(out string time, out byte brightness, out byte contrast)
    {
        time = "";
        brightness = 0;
        contrast = 0;

        if (!TimeOnly.TryParse(_timeTextBox.Text, out var parsedTime))
        {
            _statusLabel.Text = "Invalid time format. Use HH:mm (e.g. 08:00).";
            _statusLabel.ForeColor = Color.OrangeRed;
            return false;
        }

        time = parsedTime.ToString("HH:mm");
        brightness = (byte)_brightnessNumeric.Value;
        contrast = (byte)_contrastNumeric.Value;
        return true;
    }

    private async Task SetBrightnessTestAsync(byte value)
    {
        if (!TryGetSelectedMonitorHandle(out var handle)) return;

        bool success = await _displayController.SetBrightnessAsync(handle, value);
        UpdateStatus(success, $"Set brightness to {value}", $"Failed to set brightness: {_displayController.ErrorMessage}");
    }

    private async Task SetContrastTestAsync(byte value)
    {
        if (!TryGetSelectedMonitorHandle(out var handle)) return;

        bool success = await _displayController.SetContrastAsync(handle, value);
        UpdateStatus(success, $"Set contrast to {value}", $"Failed to set contrast: {_displayController.ErrorMessage}");
    }

    private async Task ReadValuesAsync()
    {
        if (!TryGetSelectedMonitorHandle(out var handle)) return;

        var brightness = await _displayController.GetBrightnessAsync(handle);
        var contrast = await _displayController.GetContrastAsync(handle);

        if (brightness.HasValue)
        {
            _readBrightnessLabel.Text = $"Brightness: {brightness.Value}";
        }
        else
        {
            _readBrightnessLabel.Text = $"Brightness: error ({_displayController.ErrorMessage})";
        }

        if (contrast.HasValue)
        {
            _readContrastLabel.Text = $"Contrast: {contrast.Value}";
        }
        else
        {
            _readContrastLabel.Text = $"Contrast: error ({_displayController.ErrorMessage})";
        }

        _statusLabel.Text = "Read monitor values.";
        _statusLabel.ForeColor = Color.ForestGreen;
    }

    private bool TryGetSelectedMonitorHandle(out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            handle = monitor.Handle;
            return true;
        }

        _statusLabel.Text = "No monitor selected.";
        _statusLabel.ForeColor = Color.OrangeRed;
        return false;
    }

    private void UpdateStatus(bool success, string successMessage, string errorMessage)
    {
        _statusLabel.Text = success ? successMessage : errorMessage;
        _statusLabel.ForeColor = success ? Color.ForestGreen : Color.OrangeRed;

        if (success && _monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            // Capture the handle on the UI thread before starting background work
            var handle = monitor.Handle;

            // Refresh read values after successful write
            _ = Task.Run(async () =>
            {
                var brightness = await _displayController.GetBrightnessAsync(handle);
                var contrast = await _displayController.GetContrastAsync(handle);

                if (IsDisposed) return;

                if (brightness.HasValue)
                    BeginInvoke(() => _readBrightnessLabel.Text = $"Brightness: {brightness.Value}");
                if (contrast.HasValue)
                    BeginInvoke(() => _readContrastLabel.Text = $"Contrast: {contrast.Value}");
            });
        }
    }
}