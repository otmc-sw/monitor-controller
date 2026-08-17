using monitor_controller.Display;

namespace monitor_controller.Scheduling;

public sealed class DisplayScheduler : IDisposable
{
    private readonly IDisplayController _displayController;
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cancellationTokenSource;
    private DisplayProfile? _lastAppliedProfile;
    private bool _enabled = true;
    private IntPtr _selectedMonitorHandle = IntPtr.Zero;

    public event EventHandler<DisplayProfile?>? ProfileChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public DisplayScheduler(IDisplayController displayController)
    {
        _displayController = displayController;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    }

    public void SetSelectedMonitor(IntPtr handle)
    {
        _selectedMonitorHandle = handle;
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

        _ = Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(token))
            {
                if (_enabled)
                {
                    await ApplyCurrentProfileAsync(profiles);
                }
            }
        }, token);
    }

    public async Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
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
                ErrorOccurred?.Invoke(this, "No monitor selected");
                return;
            }

            var activeProfile = GetActiveProfile(profiles);
            if (activeProfile == null) return;

            // Only apply if the profile has actually changed
            if (_lastAppliedProfile != null && ProfilesEqual(_lastAppliedProfile, activeProfile))
            {
                return;
            }

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
                ErrorOccurred?.Invoke(this, "No monitor selected");
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
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _timer.Dispose();
    }
}