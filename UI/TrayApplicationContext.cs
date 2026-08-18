using monitor_controller.Configuration;
using monitor_controller.Display;
using monitor_controller.Scheduling;

namespace monitor_controller.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IDisplayController _displayController;
    private readonly DisplayScheduler _scheduler;
    private readonly ConfigService _configService;
    private AppConfig _config = AppConfig.Default;
    private DisplayProfile? _currentProfile;
    private PhysicalMonitorInfo[] _monitors = Array.Empty<PhysicalMonitorInfo>();
    private ToolStripMenuItem? _profileMenuItem;
    private ToolStripMenuItem? _enableMenuItem;

    public TrayApplicationContext(
        IDisplayController displayController,
        DisplayScheduler scheduler,
        ConfigService configService)
    {
        _displayController = displayController;
        _scheduler = scheduler;
        _configService = configService;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Monitor Controller",
            Visible = true
        };

        _notifyIcon.DoubleClick += OnDoubleClick;
        _scheduler.ProfileChanged += OnProfileChanged;
        _scheduler.ErrorOccurred += OnErrorOccurred;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            _config = await _configService.LoadAsync();
            _monitors = await _displayController.EnumerateMonitorsAsync();

            // Auto-select the first available monitor if the user hasn't selected one yet
            _scheduler.SetDefaultMonitor(_monitors);

            if (_monitors.Length == 0)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Monitor Controller",
                    "No physical monitors detected. DDC/CI may be unavailable.",
                    ToolTipIcon.Warning);
            }
            else if (!_displayController.IsAvailable)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Monitor Controller Error",
                    _displayController.ErrorMessage ?? "DDC/CI is unavailable.",
                    ToolTipIcon.Error);
            }

            IntPtr selectedHandle = SelectMonitorHandle();
            _scheduler.SetSelectedMonitor(selectedHandle);

            if (selectedHandle == IntPtr.Zero && _monitors.Length > 0)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Monitor Controller",
                    "No monitor selected. Open Settings to choose a monitor.",
                    ToolTipIcon.Warning);
            }

            await _scheduler.StartAsync(_config.Profiles);
            BuildContextMenu();
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Monitor Controller Error",
                $"Failed to initialize: {ex.Message}",
                ToolTipIcon.Error);

            BuildContextMenu();
        }
    }

    private IntPtr SelectMonitorHandle()
    {
        if (_monitors.Length == 0) return IntPtr.Zero;

        // Try to restore the saved monitor by handle
        if (!string.IsNullOrEmpty(_config.SelectedMonitorHandle))
        {
            try
            {
                var savedHandle = new IntPtr(long.Parse(_config.SelectedMonitorHandle));
                var match = _monitors.FirstOrDefault(m => m.Handle == savedHandle);
                if (match != null)
                {
                    return match.Handle;
                }
            }
            catch
            {
                // Ignore parse errors and fall through to first monitor
            }
        }

        // Default to the first monitor with a valid (non-zero) handle
        var first = _monitors.FirstOrDefault(m => m.Handle != IntPtr.Zero);
        if (first == null)
        {
            return IntPtr.Zero;
        }

        _config = _config with { SelectedMonitorHandle = first.Handle.ToString() };
        _ = _configService.SaveAsync(_config);
        return first.Handle;
    }

    private void BuildContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        // Current Profile
        _profileMenuItem = new ToolStripMenuItem(
            $"Current Profile: {_currentProfile?.Time ?? "None"} (B:{_currentProfile?.Brightness ?? 0} C:{_currentProfile?.Contrast ?? 0})")
        {
            Enabled = false
        };
        contextMenu.Items.Add(_profileMenuItem);

        // Monitor info
        var monitorText = _monitors.Length == 0
            ? "Monitor: Not Detected"
            : $"Monitor: {_monitors[0].Description}";
        if (_monitors.Length > 1)
        {
            monitorText += $" (+{_monitors.Length - 1} more)";
        }
        contextMenu.Items.Add(new ToolStripMenuItem(monitorText) { Enabled = false });

        contextMenu.Items.Add(new ToolStripSeparator());

        // Enable/Disable
        _enableMenuItem = new ToolStripMenuItem(
            _scheduler.Enabled ? "Disable" : "Enable",
            null, (s, e) =>
            {
                _scheduler.Enabled = !_scheduler.Enabled;
                RefreshEnableMenuItem();
            });
        contextMenu.Items.Add(_enableMenuItem);

        // Settings
        contextMenu.Items.Add("Settings", null, (s, e) => OpenSettings());

        // Apply Current Profile
        contextMenu.Items.Add("Apply Current Profile", null, async (s, e) =>
        {
            var activeProfile = _scheduler.GetActiveProfile(_config.Profiles);
            if (activeProfile != null)
            {
                await _scheduler.ApplyProfileAsync(activeProfile);
            }
            else
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Monitor Controller",
                    "No profiles configured. Open Settings to add one.",
                    ToolTipIcon.Info);
            }
        });

        contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void RefreshEnableMenuItem()
    {
        if (_enableMenuItem != null)
        {
            _enableMenuItem.Text = _scheduler.Enabled ? "Disable" : "Enable";
        }
    }

    private void RefreshProfileMenuItem()
    {
        if (_profileMenuItem != null)
        {
            _profileMenuItem.Text = _currentProfile == null
                ? "Current Profile: None"
                : $"Current Profile: {_currentProfile.Time} (B:{_currentProfile.Brightness} C:{_currentProfile.Contrast})";
        }
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private void OnProfileChanged(object? sender, DisplayProfile? profile)
    {
        _currentProfile = profile;
        RefreshProfileMenuItem();
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        _notifyIcon.ShowBalloonTip(
            3000,
            "Monitor Controller Error",
            error,
            ToolTipIcon.Error);
    }

    private void OpenSettings()
    {
        var settingsForm = new SettingsForm(
            _displayController,
            _scheduler,
            _configService,
            _config,
            _monitors,
            _currentProfile);

        settingsForm.FormClosed += async (s, e) =>
        {
            _config = await _configService.LoadAsync();
            _monitors = await _displayController.EnumerateMonitorsAsync();

            // Auto-select the first available monitor if the user hasn't selected one yet
            _scheduler.SetDefaultMonitor(_monitors);

            // Re-select the monitor after settings changes
            _scheduler.SetSelectedMonitor(SelectMonitorHandle());

            // Restart scheduler to pick up any profile changes
            await _scheduler.StartAsync(_config.Profiles);

            BuildContextMenu();
        };

        settingsForm.Show();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _scheduler?.Dispose();
            _displayController?.Dispose();
        }
        base.Dispose(disposing);
    }
}