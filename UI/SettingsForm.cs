using System.Drawing.Drawing2D;
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
    private readonly Button _readValuesButton;
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

        Text = "Monitor Controller - Control Center";
        Width = 780;
        Height = 600;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        BackColor = Color.FromArgb(248, 249, 250);

        // Main Layout Container
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16),
            AutoScroll = true
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Monitor Selection Group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Profiles Management Group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Manual Test Group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status & Save Bar
        Controls.Add(mainLayout);

        // --- 1. MONITOR SELECTION GROUP ---
        var monitorGroup = new GroupBox
        {
            Text = " Display Selection ",
            Dock = DockStyle.Fill,
            Height = 70,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(10)
        };
        var monitorLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        _monitorComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 500,
            Font = new Font("Segoe UI", 9.5F)
        };
        monitorLayout.Controls.Add(new Label { Text = "Target Monitor: ", AutoSize = true, Margin = new Padding(0, 6, 10, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Regular) });
        monitorLayout.Controls.Add(_monitorComboBox);
        monitorGroup.Controls.Add(monitorLayout);
        mainLayout.Controls.Add(monitorGroup, 0, 0);

        // --- 2. PROFILES GROUP (Split: List on left, Inputs & Controls on right) ---
        var profileGroup = new GroupBox
        {
            Text = " Scheduled Profiles ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(10)
        };
        
        var profileSplitLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Left side: Profile ListBox
        _profileList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 0, 10, 0)
        };
        profileSplitLayout.Controls.Add(_profileList, 0, 0);

        // Right side: Inputs + Action Buttons
        var profileControlPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        profileControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        profileControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var regularFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        profileControlPanel.Controls.Add(new Label { Text = "Time (HH:mm):", Anchor = AnchorStyles.Left, Font = regularFont }, 0, 0);
        _timeTextBox = new TextBox { Dock = DockStyle.Fill, Text = "08:00", Font = regularFont };
        profileControlPanel.Controls.Add(_timeTextBox, 1, 0);

        profileControlPanel.Controls.Add(new Label { Text = "Brightness:", Anchor = AnchorStyles.Left, Font = regularFont }, 0, 1);
        _brightnessNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 50, Font = regularFont };
        profileControlPanel.Controls.Add(_brightnessNumeric, 1, 1);

        profileControlPanel.Controls.Add(new Label { Text = "Contrast:", Anchor = AnchorStyles.Left, Font = regularFont }, 0, 2);
        _contrastNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 50, Font = regularFont };
        profileControlPanel.Controls.Add(_contrastNumeric, 1, 2);

        // Buttons Panel inside right side
        var profileButtonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 10, 0, 0)
        };
        _addButton = new Button { Text = "Add", Width = 70, Height = 30, Font = regularFont };
        _editButton = new Button { Text = "Edit", Width = 70, Height = 30, Font = regularFont };
        _deleteButton = new Button { Text = "Delete", Width = 70, Height = 30, Font = regularFont };
        profileButtonsFlow.Controls.AddRange(new Control[] { _addButton, _editButton, _deleteButton });
        profileControlPanel.Controls.Add(profileButtonsFlow, 1, 3);
        profileControlPanel.SetColumnSpan(profileButtonsFlow, 2);

        // Apply Profile Button (Standalone below)
        _applyButton = new Button { Text = "Apply Profile Now", Dock = DockStyle.Fill, Height = 32, Margin = new Padding(0, 10, 0, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        profileControlPanel.Controls.Add(_applyButton, 1, 4);
        profileControlPanel.SetColumnSpan(_applyButton, 2);

        profileSplitLayout.Controls.Add(profileControlPanel, 1, 0);
        profileGroup.Controls.Add(profileSplitLayout);
        mainLayout.Controls.Add(profileGroup, 0, 1);

        // --- 3. MANUAL TEST GROUP (DDC/CI) ---
        var testGroup = new GroupBox
        {
            Text = " Manual Hardware Test (DDC/CI) ",
            Dock = DockStyle.Fill,
            Height = 120,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(10)
        };
        var testLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true
        };
        testLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        testLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var testButtonsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _setBrightness50Button = new Button { Text = "Set Brightness 50", Width = 130, Height = 28, Font = regularFont };
        _setContrast50Button = new Button { Text = "Set Contrast 50", Width = 120, Height = 28, Font = regularFont };
        _readValuesButton = new Button { Text = "Read Values", Width = 100, Height = 28, Font = regularFont };
        testButtonsFlow.Controls.AddRange(new Control[] { _setBrightness50Button, _setContrast50Button, _readValuesButton });
        testLayout.Controls.Add(testButtonsFlow, 0, 0);
        testLayout.SetColumnSpan(testButtonsFlow, 2);

        var readValuesFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        _readBrightnessLabel = new Label { Text = "Brightness: --", AutoSize = true, Margin = new Padding(0, 4, 30, 0), Font = regularFont };
        _readContrastLabel = new Label { Text = "Contrast: --", AutoSize = true, Margin = new Padding(0, 4, 0, 0), Font = regularFont };
        readValuesFlow.Controls.AddRange(new Control[] { _readBrightnessLabel, _readContrastLabel });
        testLayout.Controls.Add(readValuesFlow, 0, 1);
        testLayout.SetColumnSpan(readValuesFlow, 2);

        testGroup.Controls.Add(testLayout);
        mainLayout.Controls.Add(testGroup, 0, 2);

        // --- 4. STATUS & SAVE BAR ---
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Height = 40,
            Margin = new Padding(0, 5, 0, 0)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
            ForeColor = Color.DimGray
        };
        _saveButton = new Button
        {
            Text = "Save Configuration",
            Width = 140,
            Height = 35,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _saveButton.FlatAppearance.BorderSize = 0;

        bottomPanel.Controls.Add(_statusLabel, 0, 0);
        bottomPanel.Controls.Add(_saveButton, 1, 0);
        mainLayout.Controls.Add(bottomPanel, 0, 3);

        // Wire up events
        _profileList.SelectedIndexChanged += OnProfileSelected;
        _addButton.Click += OnAddProfile;
        _editButton.Click += OnEditProfile;
        _deleteButton.Click += OnDeleteProfile;
        _applyButton.Click += OnApplyProfile;
        _saveButton.Click += OnSave;
        _setBrightness50Button.Click += async (s, e) => await SetBrightnessTestAsync(50);
        _setContrast50Button.Click += async (s, e) => await SetContrastTestAsync(50);
        _readValuesButton.Click += async (s, e) => await ReadValuesAsync();

        LoadConfig();
    }

    private void LoadConfig()
    {
        _monitorComboBox.Items.Clear();
        foreach (var monitor in _monitors)
        {
            _monitorComboBox.Items.Add(monitor);
        }

        if (_monitors.Length > 0)
        {
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
                    // Fall back to first monitor
                }
            }
            _monitorComboBox.SelectedIndex = selectedIndex;
        }

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
            _profileList.Items.Add($"{profile.Time}    |    Brightness: {profile.Brightness}%    |    Contrast: {profile.Contrast}%{marker}");
        }

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
        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast)) return;

        if (_config.Profiles.Any(p => p.Time == time))
        {
            _statusLabel.Text = "A profile with this time already exists.";
            _statusLabel.ForeColor = Color.Firebrick;
            return;
        }

        _config.Profiles.Add(new DisplayProfile(time, brightness, contrast));
        _statusLabel.Text = $"Profile added: {time}";
        _statusLabel.ForeColor = Color.SeaGreen;
        RefreshProfileList();
    }

    private void OnEditProfile(object? sender, EventArgs e)
    {
        var profiles = _config.Profiles.OrderBy(p => p.TimeOnly).ToArray();
        int index = _profileList.SelectedIndex;
        if (index < 0 || index >= profiles.Length) return;

        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast)) return;

        if (_config.Profiles.Any(p => p.Time == time && p.Time != profiles[index].Time))
        {
            _statusLabel.Text = "A profile with this time already exists.";
            _statusLabel.ForeColor = Color.Firebrick;
            return;
        }

        _config.Profiles.Remove(profiles[index]);
        _config.Profiles.Add(new DisplayProfile(time, brightness, contrast));
        _statusLabel.Text = $"Profile edited: {time}";
        _statusLabel.ForeColor = Color.SeaGreen;
        RefreshProfileList();
    }

    private void OnDeleteProfile(object? sender, EventArgs e)
    {
        var profiles = _config.Profiles.OrderBy(p => p.TimeOnly).ToArray();
        int index = _profileList.SelectedIndex;
        if (index < 0 || index >= profiles.Length) return;

        var toDelete = profiles[index];
        if (MessageBox.Show(
                $"Delete profile '{toDelete.Time}' (Brightness: {toDelete.Brightness}, Contrast: {toDelete.Contrast})?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _config.Profiles.Remove(toDelete);
            _statusLabel.Text = $"Profile deleted: {toDelete.Time}";
            _statusLabel.ForeColor = Color.SeaGreen;
            RefreshProfileList();
        }
    }

    private async void OnApplyProfile(object? sender, EventArgs e)
    {
        if (!TryParseCurrentValues(out var time, out byte brightness, out byte contrast)) return;

        var profile = new DisplayProfile(time, brightness, contrast);
        await _scheduler.ApplyProfileAsync(profile);
        _statusLabel.Text = $"Applied profile: {time} (B:{brightness} C:{contrast})";
        _statusLabel.ForeColor = Color.SeaGreen;
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

        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            _scheduler.SetSelectedMonitor(monitor.Handle);
        }

        _statusLabel.Text = "Configuration saved successfully.";
        _statusLabel.ForeColor = Color.SeaGreen;
    }

    private bool TryParseCurrentValues(out string time, out byte brightness, out byte contrast)
    {
        time = "";
        brightness = 0;
        contrast = 0;

        if (!TimeOnly.TryParse(_timeTextBox.Text, out var parsedTime))
        {
            _statusLabel.Text = "Invalid time format. Use HH:mm (e.g. 08:00).";
            _statusLabel.ForeColor = Color.Firebrick;
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

        _readBrightnessLabel.Text = brightness.HasValue ? $"Brightness: {brightness.Value}" : $"Brightness: error";
        _readContrastLabel.Text = contrast.HasValue ? $"Contrast: {contrast.Value}" : $"Contrast: error";

        _statusLabel.Text = "Read monitor values successfully.";
        _statusLabel.ForeColor = Color.SeaGreen;
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
        _statusLabel.ForeColor = Color.Firebrick;
        return false;
    }

    private void UpdateStatus(bool success, string successMessage, string errorMessage)
    {
        _statusLabel.Text = success ? successMessage : errorMessage;
        _statusLabel.ForeColor = success ? Color.SeaGreen : Color.Firebrick;

        if (success && _monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            var handle = monitor.Handle;
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