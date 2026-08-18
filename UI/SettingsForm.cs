using monitor_controller.Configuration;
using monitor_controller.Display;
using monitor_controller.Scheduling;

namespace monitor_controller.UI;

public partial class SettingsForm : Form
{
    private readonly IDisplayController _displayController;
    private readonly DisplayScheduler _scheduler;
    private readonly ConfigService _configService;
    private AppConfig _config;
    private readonly PhysicalMonitorInfo[] _monitors;
    private readonly DisplayProfile? _currentProfile;

    // Controls
    private readonly ComboBox _monitorComboBox;

    // Manual Controls
    private readonly TrackBar _manualBrightnessTrackBar;
    private readonly TrackBar _manualContrastTrackBar;
    private readonly Label _manualBrightnessValueLabel;
    private readonly Label _manualContrastValueLabel;

    // Debounce CancellationTokenSources for manual sliders
    private CancellationTokenSource? _brightnessCts;
    private CancellationTokenSource? _contrastCts;

    // Profile Controls
    private bool _isSchedulerEnabled = true;
    private readonly Button _schedulerToggleButton;
    private readonly TableLayoutPanel _profileSplitLayout;
    private readonly ListBox _profileList;
    private readonly TextBox _timeTextBox;
    private readonly TrackBar _profileBrightnessTrackBar;
    private readonly TrackBar _profileContrastTrackBar;
    private readonly Label _profileBrightnessValueLabel;
    private readonly Label _profileContrastValueLabel;

    private readonly Button _addButton;
    private readonly Button _editButton;
    private readonly Button _deleteButton;
    private readonly Button _applyButton;

    // Bottom Status & Save & Reset
    private readonly Label _statusLabel;
    private readonly Button _resetButton;
    private readonly Button _saveButton;

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

        InitializeComponent(
            out _monitorComboBox,
            out _manualBrightnessTrackBar,
            out _manualContrastTrackBar,
            out _manualBrightnessValueLabel,
            out _manualContrastValueLabel,
            out _schedulerToggleButton,
            out _profileSplitLayout,
            out _profileList,
            out _timeTextBox,
            out _profileBrightnessTrackBar,
            out _profileContrastTrackBar,
            out _profileBrightnessValueLabel,
            out _profileContrastValueLabel,
            out _addButton,
            out _editButton,
            out _deleteButton,
            out _applyButton,
            out _resetButton,
            out _statusLabel,
            out _saveButton
        );

        // Wire up scheduler toggle button
        _schedulerToggleButton.Click += (s, e) =>
        {
            _isSchedulerEnabled = !_isSchedulerEnabled;
            UpdateSchedulerPanelState();
        };

        // Wire up manual slider events
        _manualBrightnessTrackBar.ValueChanged += OnManualBrightnessChanged;
        _manualContrastTrackBar.ValueChanged += OnManualContrastChanged;

        // Wire up profile slider events (only updates UI label)
        _profileBrightnessTrackBar.ValueChanged += (s, e) => _profileBrightnessValueLabel.Text = _profileBrightnessTrackBar.Value.ToString();
        _profileContrastTrackBar.ValueChanged += (s, e) => _profileContrastValueLabel.Text = _profileContrastTrackBar.Value.ToString();

        // Wire up profile management events
        _profileList.SelectedIndexChanged += OnProfileSelected;
        _addButton.Click += OnAddProfile;
        _editButton.Click += OnEditProfile;
        _deleteButton.Click += OnDeleteProfile;
        _applyButton.Click += OnApplyProfile;
        _resetButton.Click += OnResetDefault;
        _saveButton.Click += OnSave;

        LoadConfig();
        InitializeManualSliders();
    }

    private void InitializeManualSliders()
    {
        // Try reading initial values asynchronously without blocking
        _ = Task.Run(async () =>
        {
            if (!TryGetSelectedMonitorHandle(out var handle, silent: true)) return;

            var brightness = await _displayController.GetBrightnessAsync(handle);
            var contrast = await _displayController.GetContrastAsync(handle);

            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                if (brightness.HasValue)
                {
                    int val = Math.Clamp((int)brightness.Value, 0, 100);
                    _manualBrightnessTrackBar.Value = val;
                    _manualBrightnessValueLabel.Text = val.ToString();
                }
                if (contrast.HasValue)
                {
                    int val = Math.Clamp((int)contrast.Value, 0, 100);
                    _manualContrastTrackBar.Value = val;
                    _manualContrastValueLabel.Text = val.ToString();
                }
            });
        });
    }

    private void OnManualBrightnessChanged(object? sender, EventArgs e)
    {
        byte val = (byte)_manualBrightnessTrackBar.Value;
        _manualBrightnessValueLabel.Text = val.ToString();

        _brightnessCts?.Cancel();
        _brightnessCts?.Dispose();
        _brightnessCts = new CancellationTokenSource();
        var token = _brightnessCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!TryGetSelectedMonitorHandle(out var handle, silent: false)) return;

                bool success = await _displayController.SetBrightnessAsync(handle, val);
                if (IsDisposed) return;

                BeginInvoke(() =>
                {
                    if (success)
                    {
                        _statusLabel.Text = $"Brightness set to {val}";
                        _statusLabel.ForeColor = Color.SeaGreen;
                    }
                    else
                    {
                        _statusLabel.Text = $"Failed to set brightness: {_displayController.ErrorMessage}";
                        _statusLabel.ForeColor = Color.Firebrick;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // Debounced request replaced by a newer value
            }
        }, token);
    }

    private void OnManualContrastChanged(object? sender, EventArgs e)
    {
        byte val = (byte)_manualContrastTrackBar.Value;
        _manualContrastValueLabel.Text = val.ToString();

        _contrastCts?.Cancel();
        _contrastCts?.Dispose();
        _contrastCts = new CancellationTokenSource();
        var token = _contrastCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!TryGetSelectedMonitorHandle(out var handle, silent: false)) return;

                bool success = await _displayController.SetContrastAsync(handle, val);
                if (IsDisposed) return;

                BeginInvoke(() =>
                {
                    if (success)
                    {
                        _statusLabel.Text = $"Contrast set to {val}";
                        _statusLabel.ForeColor = Color.SeaGreen;
                    }
                    else
                    {
                        _statusLabel.Text = $"Failed to set contrast: {_displayController.ErrorMessage}";
                        _statusLabel.ForeColor = Color.Firebrick;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // Debounced request replaced by a newer value
            }
        }, token);
    }

    private bool TryGetSelectedMonitorHandle(out IntPtr handle, bool silent = false)
    {
        handle = IntPtr.Zero;
        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo monitor)
        {
            handle = monitor.Handle;
            if (handle != IntPtr.Zero) return true;
        }

        if (!silent && !IsDisposed)
        {
            BeginInvoke(() =>
            {
                _statusLabel.Text = "No physical monitor available.";
                _statusLabel.ForeColor = Color.Firebrick;
            });
        }
        return false;
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

        _isSchedulerEnabled = _config.IsSchedulerEnabled;
        UpdateSchedulerPanelState();

        RefreshProfileList();
    }

    private void UpdateSchedulerPanelState()
    {
        _profileSplitLayout.Enabled = _isSchedulerEnabled;
        if (_isSchedulerEnabled)
        {
            _schedulerToggleButton.Text = "Enabled";
            _schedulerToggleButton.BackColor = Color.FromArgb(0, 120, 212);
            _schedulerToggleButton.ForeColor = Color.White;
            _schedulerToggleButton.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
        }
        else
        {
            _schedulerToggleButton.Text = "Disabled";
            _schedulerToggleButton.BackColor = Color.FromArgb(241, 243, 245);
            _schedulerToggleButton.ForeColor = Color.FromArgb(108, 117, 125);
            _schedulerToggleButton.FlatAppearance.BorderColor = Color.FromArgb(222, 226, 230);
        }
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
            string marker = isActive ? "✅ ACTIVE" : "";
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
        _profileBrightnessTrackBar.Value = Math.Clamp((int)profile.Brightness, 0, 100);
        _profileContrastTrackBar.Value = Math.Clamp((int)profile.Contrast, 0, 100);
        _profileBrightnessValueLabel.Text = profile.Brightness.ToString();
        _profileContrastValueLabel.Text = profile.Contrast.ToString();

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

    private void OnResetDefault(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
                "Reset all profiles to default schedule?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            var defaultProfiles = AppConfig.Default.Profiles;
            _config.Profiles.Clear();
            _config.Profiles.AddRange(defaultProfiles);
            RefreshProfileList();
            _statusLabel.Text = "Profiles reset to default.";
            _statusLabel.ForeColor = Color.SeaGreen;
        }
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        string? selectedMonitorHandle = null;
        if (_monitorComboBox.SelectedItem is PhysicalMonitorInfo selectedMonitor)
        {
            selectedMonitorHandle = selectedMonitor.Handle.ToString();
        }

        _config = _config with 
        { 
            SelectedMonitorHandle = selectedMonitorHandle,
            IsFirstRun = false,
            IsSchedulerEnabled = _isSchedulerEnabled
        };
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
        brightness = (byte)_profileBrightnessTrackBar.Value;
        contrast = (byte)_profileContrastTrackBar.Value;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _brightnessCts?.Cancel();
            _brightnessCts?.Dispose();
            _contrastCts?.Cancel();
            _contrastCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}