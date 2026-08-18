using monitor_controller.Display;
using monitor_controller.Infrastructure;

namespace monitor_controller.Scheduling;

public sealed class DisplayScheduler : IDisposable
{
    private readonly IDisplayController _displayController;
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;
    private DisplayProfile? _lastAppliedProfile;
    private bool _enabled = true;
    private bool _disposed;
    private IntPtr _selectedMonitorHandle = IntPtr.Zero;

    public event EventHandler<DisplayProfile?>? ProfileChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public bool HasSelectedMonitor =>
        _selectedMonitorHandle != IntPtr.Zero;

    public DisplayScheduler(IDisplayController displayController)
    {
        _displayController = displayController;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    }

    public void SetSelectedMonitor(IntPtr handle)
    {
        // Do not override a valid selection with a zero handle
        if (handle == IntPtr.Zero && _selectedMonitorHandle != IntPtr.Zero)
        {
            return;
        }

        if (_selectedMonitorHandle != handle)
        {
            _selectedMonitorHandle = handle;
            // Force re-apply on next check so the profile is applied to the new monitor
            _lastAppliedProfile = null;

            if (handle != IntPtr.Zero)
            {
                Logger.Info($"Selected monitor: Handle=0x{handle.ToInt64():X}");
            }
        }
    }

    /// <summary>
    /// Automatically selects the first available physical monitor if no monitor
    /// has been explicitly selected yet. Preserves an existing selection.
    /// </summary>
    public void SetDefaultMonitor(IEnumerable<PhysicalMonitorInfo> monitors)
    {
        if (_selectedMonitorHandle != IntPtr.Zero)
        {
            // Keep the existing selection
            return;
        }

        var monitorList = monitors.ToList();
        if (monitorList.Count == 0)
        {
            Logger.Warning("No physical monitor available for scheduler.");
            return;
        }

        // Find the first monitor with a valid (non-zero) handle
        var first = monitorList.FirstOrDefault(m => m.Handle != IntPtr.Zero);
        if (first == null)
        {
            Logger.Warning("No physical monitor with a valid handle available for scheduler.");
            return;
        }

        _selectedMonitorHandle = first.Handle;
        _lastAppliedProfile = null;

        Logger.Info(
            $"Automatically selected monitor: {first.Description}, Handle=0x{first.Handle.ToInt64():X}");
    }

    /// <summary>
    /// Determines the active profile for the current local time.
    /// The active profile is the most recent profile whose time is <= now.
    /// Wraps around midnight: before the first profile time, the last profile of the day remains active.
    /// </summary>
    public DisplayProfile? GetActiveProfile(List<DisplayProfile> profiles)
    {
        if (profiles.Count == 0) return null;

        var now = TimeOnly.FromDateTime(DateTime.Now);

        // Sort by time to ensure deterministic ordering
        var ordered = profiles.OrderBy(p => p.TimeOnly).ToArray();

        // Find the most recent profile with time <= now
        DisplayProfile? activeProfile = null;
        foreach (var profile in ordered)
        {
            if (profile.TimeOnly <= now)
            {
                activeProfile = profile;
            }
        }

        // If no profile has triggered yet today (now is before the earliest profile),
        // wrap around to the last profile of the day (e.g. 23:00 remains active until 06:00).
        if (activeProfile == null)
        {
            activeProfile = ordered[^1];
        }

        return activeProfile;
    }

    public async Task StartAsync(List<DisplayProfile> profiles)
    {
        if (_cancellationTokenSource != null)
        {
            await StopAsync();
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        // Apply current profile immediately on startup
        await ApplyCurrentProfileAsync(profiles);

        _backgroundTask = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    if (_enabled)
                    {
                        await ApplyCurrentProfileAsync(profiles);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Scheduler error: {ex.Message}");
            }
        }, token);
    }

    public async Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _backgroundTask = null;
    }

    private async Task ApplyCurrentProfileAsync(List<DisplayProfile> profiles)
    {
        try
        {
            if (!_displayController.IsAvailable)
            {
                ErrorOccurred?.Invoke(this, _displayController.ErrorMessage ?? "Display controller unavailable");
                return;
            }

            if (_selectedMonitorHandle == IntPtr.Zero)
            {
                Logger.Warning("No physical monitor available for scheduler.");
                ErrorOccurred?.Invoke(this, "No physical monitor available.");
                return;
            }

            var activeProfile = GetActiveProfile(profiles);
            if (activeProfile == null) return;

            Logger.Info($"Active profile: {activeProfile.Time}");

            // Only apply if the profile has actually changed
            if (_lastAppliedProfile != null && ProfilesEqual(_lastAppliedProfile, activeProfile))
            {
                Logger.Info("Profile unchanged, skipping apply.");
                return;
            }

            Logger.Info(
                $"Active profile changed: {activeProfile.Time}. " +
                $"Applying brightness={activeProfile.Brightness}, contrast={activeProfile.Contrast}");

            await ApplyProfileAsync(activeProfile);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error applying profile: {ex.Message}");
        }
    }

    public async Task ApplyProfileAsync(DisplayProfile profile)
    {
        try
        {
            if (!_displayController.IsAvailable)
            {
                ErrorOccurred?.Invoke(this, _displayController.ErrorMessage ?? "Display controller unavailable");
                return;
            }

            if (_selectedMonitorHandle == IntPtr.Zero)
            {
                Logger.Warning("No physical monitor available for scheduler.");
                ErrorOccurred?.Invoke(this, "No physical monitor available.");
                return;
            }

            bool brightnessSet = await _displayController.SetBrightnessAsync(_selectedMonitorHandle, profile.Brightness);
            bool contrastSet = await _displayController.SetContrastAsync(_selectedMonitorHandle, profile.Contrast);

            if (brightnessSet && contrastSet)
            {
                _lastAppliedProfile = profile;
                ProfileChanged?.Invoke(this, profile);
            }
            else
            {
                ErrorOccurred?.Invoke(this, $"Failed to apply profile: {_displayController.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error applying profile: {ex.Message}");
        }
    }

    private static bool ProfilesEqual(DisplayProfile a, DisplayProfile b)
    {
        return a.Time == b.Time &&
               a.Brightness == b.Brightness &&
               a.Contrast == b.Contrast;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _timer.Dispose();
    }
}