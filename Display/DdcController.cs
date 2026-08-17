using System.Runtime.InteropServices;

namespace monitor_controller.Display;

public sealed class DdcController : IDisplayController
{
    private const string DllName = "dxva2.dll";

    // VCP codes
    private const byte VCP_BRIGHTNESS = 0x10;
    private const byte VCP_CONTRAST = 0x12;

    private bool _disposed;
    private string? _errorMessage;
    private readonly List<PHYSICAL_MONITOR> _physicalMonitors = new();

    public bool IsAvailable => _errorMessage == null;
    public string? ErrorMessage => _errorMessage;

    #region P/Invoke Declarations

    [DllImport(DllName)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport(DllName)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    [DllImport(DllName)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport(DllName)]
    private static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

    [DllImport(DllName)]
    private static extern bool DestroyPhysicalMonitors(
        uint dwPhysicalMonitorArraySize,
        [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport(DllName)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(
        IntPtr hMonitor,
        byte bVCPCode,
        out IntPtr pvct,
        out uint pdwCurrentValue,
        out uint pdwMaximumValue);

    [DllImport(DllName)]
    private static extern bool SetVCPFeature(
        IntPtr hMonitor,
        byte bVCPCode,
        uint dwNewValue);

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor,
        IntPtr hdcMonitor,
        IntPtr lprcMonitor,
        IntPtr dwData);

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    #endregion

    public async Task<PhysicalMonitorInfo[]> EnumerateMonitorsAsync()
    {
        return await Task.Run(() =>
        {
            // Release previously acquired handles before re-enumerating
            ReleaseMonitors();

            var monitors = new List<PhysicalMonitorInfo>();

            bool callback(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
            {
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numMonitors))
                {
                    return true;
                }

                var physMonitors = new PHYSICAL_MONITOR[numMonitors];
                if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, numMonitors, physMonitors))
                {
                    return true;
                }

                for (int i = 0; i < numMonitors; i++)
                {
                    var physMonitor = physMonitors[i];
                    _physicalMonitors.Add(physMonitor);
                    monitors.Add(new PhysicalMonitorInfo(
                        physMonitor.hPhysicalMonitor,
                        physMonitor.szPhysicalMonitorDescription,
                        new HMonitor(hMonitor))
                    {
                        Id = physMonitor.hPhysicalMonitor.ToString()
                    });
                }

                return true;
            }

            try
            {
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
                _errorMessage = null;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to enumerate monitors: {ex.Message}";
            }

            return monitors.ToArray();
        });
    }

    public async Task<bool> SetBrightnessAsync(IntPtr monitorHandle, byte value)
    {
        return await SetVcpValueAsync(monitorHandle, VCP_BRIGHTNESS, value);
    }

    public async Task<byte?> GetBrightnessAsync(IntPtr monitorHandle)
    {
        return await GetVcpValueAsync(monitorHandle, VCP_BRIGHTNESS);
    }

    public async Task<bool> SetContrastAsync(IntPtr monitorHandle, byte value)
    {
        return await SetVcpValueAsync(monitorHandle, VCP_CONTRAST, value);
    }

    public async Task<byte?> GetContrastAsync(IntPtr monitorHandle)
    {
        return await GetVcpValueAsync(monitorHandle, VCP_CONTRAST);
    }

    private async Task<bool> SetVcpValueAsync(IntPtr monitorHandle, byte vcpCode, byte value)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!SetVCPFeature(monitorHandle, vcpCode, value))
                {
                    _errorMessage = $"Failed to set VCP code 0x{vcpCode:X2}";
                    return false;
                }

                _errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Exception setting VCP code 0x{vcpCode:X2}: {ex.Message}";
                return false;
            }
        });
    }

    private async Task<byte?> GetVcpValueAsync(IntPtr monitorHandle, byte vcpCode)
    {
        return await Task.Run<byte?>(() =>
        {
            try
            {
                if (!GetVCPFeatureAndVCPFeatureReply(
                    monitorHandle,
                    vcpCode,
                    out IntPtr pvct,
                    out uint currentValue,
                    out uint maxValue))
                {
                    _errorMessage = $"Failed to get VCP code 0x{vcpCode:X2}";
                    return null;
                }

                _errorMessage = null;
                return (byte)currentValue;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Exception getting VCP code 0x{vcpCode:X2}: {ex.Message}";
                return null;
            }
        });
    }

    private void ReleaseMonitors()
    {
        if (_physicalMonitors.Count > 0)
        {
            try
            {
                DestroyPhysicalMonitors(
                    (uint)_physicalMonitors.Count,
                    _physicalMonitors.ToArray());
            }
            catch
            {
                // Fall back to individual destruction
                foreach (var monitor in _physicalMonitors)
                {
                    try
                    {
                        DestroyPhysicalMonitor(monitor.hPhysicalMonitor);
                    }
                    catch
                    {
                        // Ignore individual disposal errors
                    }
                }
            }

            _physicalMonitors.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        ReleaseMonitors();
        _disposed = true;
    }
}