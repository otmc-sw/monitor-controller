using System.ComponentModel;
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

    // Bottom Status & Save
    private readonly Label _statusLabel;
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

        Text = "OTMC Monitor Controller";
        Width = 780;
        Height = 680;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        BackColor = Color.FromArgb(243, 243, 243);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20),
            AutoScroll = true
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header Card
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Display Selection Card
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Manual Control Card
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Scheduled Profiles Card
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status Bar
        Controls.Add(mainLayout);

        var regularFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        var boldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        var titleFont = new Font("Segoe UI", 13F, FontStyle.Bold);
        var sectionTitleFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        var subTextFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        
        // --- 2. DISPLAY SELECTION CARD ---
        var displayCard = CreateCardPanel();
        var displayLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true
        };
        displayLayout.Controls.Add(new Label
        {
            Text = "Display",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        displayLayout.Controls.Add(new Label
        {
            Text = "Target monitor",
            Font = subTextFont,
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 1);

        _monitorComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Font = regularFont,
            Height = 32
        };
        displayLayout.Controls.Add(_monitorComboBox, 0, 2);
        displayCard.Controls.Add(displayLayout);
        mainLayout.Controls.Add(displayCard, 0, 1);

        // --- 3. MANUAL CONTROL CARD ---
        var manualCard = CreateCardPanel();
        var manualLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        manualLayout.Controls.Add(new Label
        {
            Text = "Manual Control",
            Font = sectionTitleFont,
            Height = 150,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);
        manualLayout.SetColumnSpan(manualLayout.GetControlFromPosition(0, 0)!, 2);

        // Brightness row
        manualLayout.Controls.Add(new Label { Text = "Brightness", Font = regularFont, AutoSize = true }, 0, 1);
        _manualBrightnessValueLabel = new Label { Text = "60", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right };
        manualLayout.Controls.Add(_manualBrightnessValueLabel, 1, 1);

        _manualBrightnessTrackBar = CreateTrackBar();
        manualLayout.Controls.Add(_manualBrightnessTrackBar, 0, 2);
        manualLayout.SetColumnSpan(_manualBrightnessTrackBar, 2);

        // Contrast row
        manualLayout.Controls.Add(new Label { Text = "Contrast", Font = regularFont, AutoSize = true, Margin = new Padding(0, 10, 0, 0) }, 0, 3);
        _manualContrastValueLabel = new Label { Text = "30", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 10, 0, 0) };
        manualLayout.Controls.Add(_manualContrastValueLabel, 1, 3);

        _manualContrastTrackBar = CreateTrackBar();
        manualLayout.Controls.Add(_manualContrastTrackBar, 0, 4);
        manualLayout.SetColumnSpan(_manualContrastTrackBar, 2);

        manualCard.Controls.Add(manualLayout);
        mainLayout.Controls.Add(manualCard, 0, 2);

        // --- 4. SCHEDULED PROFILES CARD ---
        var profileCard = CreateCardPanel();
        var profileCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        profileCardLayout.Controls.Add(new Label
        {
            Text = "Scheduled Profiles",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var profileSplitLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        // Left side: ListBox
        _profileList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = regularFont,
            Margin = new Padding(0, 0, 10, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        profileSplitLayout.Controls.Add(_profileList, 0, 0);

        // Right side: Profile Editor
        var profileEditorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            AutoSize = true
        };
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        profileEditorPanel.Controls.Add(new Label { Text = "Time (HH:mm)", Font = regularFont, AutoSize = true }, 0, 0);
        _timeTextBox = new TextBox { Dock = DockStyle.Fill, Text = "08:00", Font = regularFont, Margin = new Padding(0, 2, 0, 8) };
        profileEditorPanel.Controls.Add(_timeTextBox, 0, 1);
        profileEditorPanel.SetColumnSpan(_timeTextBox, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Brightness", Font = regularFont, AutoSize = true }, 0, 2);
        _profileBrightnessValueLabel = new Label { Text = "50", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right };
        profileEditorPanel.Controls.Add(_profileBrightnessValueLabel, 1, 2);

        _profileBrightnessTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(_profileBrightnessTrackBar, 0, 3);
        profileEditorPanel.SetColumnSpan(_profileBrightnessTrackBar, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Contrast", Font = regularFont, AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 4);
        _profileContrastValueLabel = new Label { Text = "50", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) };
        profileEditorPanel.Controls.Add(_profileContrastValueLabel, 1, 4);

        _profileContrastTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(_profileContrastTrackBar, 0, 5);
        profileEditorPanel.SetColumnSpan(_profileContrastTrackBar, 2);

        // Action Buttons: Add, Edit, Delete
        var actionButtonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 10, 0, 0)
        };
        _addButton = CreateRoundedButton("Add", 70, 30, false);
        _editButton = CreateRoundedButton("Edit", 70, 30, false);
        _deleteButton = CreateRoundedButton("Delete", 70, 30, false);
        actionButtonsFlow.Controls.AddRange(new Control[] { _addButton, _editButton, _deleteButton });
        profileEditorPanel.Controls.Add(actionButtonsFlow, 0, 6);
        profileEditorPanel.SetColumnSpan(actionButtonsFlow, 2);

        // Apply Now Button
        _applyButton = CreateRoundedButton("Apply Now", 0, 34, false);
        _applyButton.Dock = DockStyle.Fill;
        _applyButton.Margin = new Padding(0, 8, 0, 0);
        profileEditorPanel.Controls.Add(_applyButton, 0, 7);
        profileEditorPanel.SetColumnSpan(_applyButton, 2);

        profileSplitLayout.Controls.Add(profileEditorPanel, 1, 0);
        profileCardLayout.Controls.Add(profileSplitLayout, 0, 1);
        profileCard.Controls.Add(profileCardLayout);
        mainLayout.Controls.Add(profileCard, 0, 3);

        // --- 5. STATUS BAR & SAVE BUTTON ---
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Height = 40,
            Margin = new Padding(0, 8, 0, 0)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
            ForeColor = Color.FromArgb(90, 90, 90)
        };
        _saveButton = CreateRoundedButton("Save Configuration", 150, 36, true);

        bottomPanel.Controls.Add(_statusLabel, 0, 0);
        bottomPanel.Controls.Add(_saveButton, 1, 0);
        mainLayout.Controls.Add(bottomPanel, 0, 4);

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
        _saveButton.Click += OnSave;

        LoadConfig();
        InitializeManualSliders();
    }

    private static TrackBar CreateTrackBar()
    {
        return new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 1,
            Dock = DockStyle.Fill,
            Height = 45
        };
    }

    private static RoundedPanel CreateCardPanel()
    {
        return new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.White,
            BorderColor = Color.FromArgb(225, 225, 225),
            CornerRadius = 8
        };
    }

    private static RoundedButton CreateRoundedButton(string text, int width, int height, bool isPrimary)
    {
        var btn = new RoundedButton
        {
            Text = text,
            Height = height,
            Font = new Font("Segoe UI", 9.5F, isPrimary ? FontStyle.Bold : FontStyle.Regular),
            CornerRadius = 6,
            BorderColor = isPrimary ? Color.FromArgb(0, 108, 190) : Color.FromArgb(200, 200, 200),
            NormalColor = isPrimary ? Color.FromArgb(0, 120, 212) : Color.FromArgb(245, 245, 245),
            HoverColor = isPrimary ? Color.FromArgb(16, 110, 190) : Color.FromArgb(235, 235, 235),
            ForeColor = isPrimary ? Color.White : Color.FromArgb(32, 32, 32)
        };
        if (width > 0) btn.Width = width;
        return btn;
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

public class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 8;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.LightGray;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = GetRoundedPath(rect, CornerRadius);

        using var bgBrush = new SolidBrush(BackColor);
        e.Graphics.FillPath(bgBrush, path);

        using var borderPen = new Pen(BorderColor, 1);
        e.Graphics.DrawPath(borderPen, path);
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public class RoundedButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 6;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.Gray;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color NormalColor { get; set; } = Color.White;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.LightGray;

    private bool _isHovered;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = GetRoundedPath(rect, CornerRadius);

        var fillColor = _isHovered ? HoverColor : NormalColor;
        using var bgBrush = new SolidBrush(fillColor);
        e.Graphics.FillPath(bgBrush, path);

        using var borderPen = new Pen(BorderColor, 1);
        e.Graphics.DrawPath(borderPen, path);

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor, flags);
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}