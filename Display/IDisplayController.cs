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

public record PhysicalMonitorInfo(
    IntPtr Handle,
    string Description,
    HMonitor HMonitor
);

public record HMonitor(IntPtr Handle);
