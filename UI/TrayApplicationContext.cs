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
    private AppConfig _config;
    private DisplayProfile? _currentProfile;
    private PhysicalMonitorInfo[] _monitors = Array.Empty<PhysicalMonitorInfo>();

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
            Text = "MonitorController",
            Visible = true
        };

        _notifyIcon.DoubleClick += OnDoubleClick;
        _scheduler.ProfileChanged += OnProfileChanged;
        _scheduler.ErrorOccurred += OnErrorOccurred;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        _config = await _configService.LoadAsync();
        _monitors = await _displayController.EnumerateMonitorsAsync();

        // Select monitor from config or first available
        IntPtr selectedHandle = IntPtr.Zero;
        if (!string.IsNullOrEmpty(_config.SelectedMonitorHandle))
        {
            var handle = new IntPtr(long.Parse(_config.SelectedMonitorHandle));
            if (_monitors.Any(m => m.Handle == handle))
            {
                selectedHandle = handle;
            }
        }

        if (selectedHandle == IntPtr.Zero && _monitors.Length > 0)
        {
            selectedHandle = _monitors[0].Handle;
            _config = _config with { SelectedMonitorHandle = selectedHandle.ToString() };
            await _configService.SaveAsync(_config);
        }

        _scheduler.SetSelectedMonitor(selectedHandle);
        await _scheduler.StartAsync(_config.Profiles);

        BuildContextMenu();
    }

    private void BuildContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        // Current Profile
        var profileItem = new ToolStripMenuItem(
            $"Current Profile: {_currentProfile?.Time ?? "None"} ({_currentProfile?.Brightness ?? 0}/{_currentProfile?.Contrast ?? 0})")
        {
            Enabled = false
        };
        contextMenu.Items.Add(profileItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // Enable/Disable
        var enableItem = new ToolStripMenuItem(
            _scheduler.Enabled ? "Disable" : "Enable",
            null, async (s, e) =>
            {
                _scheduler.Enabled = !_scheduler.Enabled;
                ((ToolStripMenuItem)s!).Text = _scheduler.Enabled ? "Disable" : "Enable";
            });
        contextMenu.Items.Add(enableItem);

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
        });

        contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private void OnProfileChanged(object? sender, DisplayProfile? profile)
    {
        _currentProfile = profile;
        BuildContextMenu();
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        _notifyIcon.ShowBalloonTip(
            3000,
            "MonitorController Error",
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
            BuildContextMenu();
        };

        settingsForm.Show();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _scheduler.Dispose();
        _displayController.Dispose();
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
