using monitor_controller.Display;
using monitor_controller.Scheduling;

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

    public DisplayProfile? GetActiveProfile(List<DisplayProfile> profiles)
    {
        if (profiles.Count == 0) return null;

        var now = TimeOnly.FromDateTime(DateTime.Now);
        DisplayProfile? activeProfile = null;
        TimeOnly latestTime = default;

        foreach (var profile in profiles)
        {
            if (profile.TimeOnly <= now && profile.TimeOnly > latestTime)
            {
                latestTime = profile.TimeOnly;
                activeProfile = profile;
            }
        }

        // If no profile found for today (before first profile), use the last profile from yesterday
        if (activeProfile == null && profiles.Count > 0)
        {
            activeProfile = profiles.OrderByDescending(p => p.TimeOnly).First();
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

            // Only apply if profile changed
            if (_lastAppliedProfile != null &&
                _lastAppliedProfile.Time == activeProfile.Time &&
                _lastAppliedProfile.Brightness == activeProfile.Brightness &&
                _lastAppliedProfile.Contrast == activeProfile.Contrast)
            {
                return;
            }

            bool brightnessSet = await _displayController.SetBrightnessAsync(_selectedMonitorHandle, activeProfile.Brightness);
            bool contrastSet = await _displayController.SetContrastAsync(_selectedMonitorHandle, activeProfile.Contrast);

            if (brightnessSet && contrastSet)
            {
                _lastAppliedProfile = activeProfile;
                ProfileChanged?.Invoke(this, activeProfile);
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

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _timer.Dispose();
    }
}
