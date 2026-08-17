namespace monitor_controller.Display;

public interface IDisplayController : IDisposable
{
    bool IsAvailable { get; }
    string? ErrorMessage { get; }
    
    Task<PhysicalMonitorInfo[]> EnumerateMonitorsAsync();
    Task<bool> SetBrightnessAsync(IntPtr monitorHandle, byte value);
    Task<byte?> GetBrightnessAsync(IntPtr monitorHandle);
    Task<bool> SetContrastAsync(IntPtr monitorHandle, byte value);
    Task<byte?> GetContrastAsync(IntPtr monitorHandle);
}

public sealed record PhysicalMonitorInfo(
    IntPtr Handle,
    string Description,
    HMonitor HMonitor)
{
    /// <summary>
    /// Stable identifier for persisting monitor selection across sessions.
    /// Uses the description plus an ordinal when descriptions are duplicated.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Friendly display name shown in the UI.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Id)
        ? Description
        : $"{Description} ({Id})";

    public override string ToString() => DisplayName;
}

public sealed record HMonitor(IntPtr Handle);