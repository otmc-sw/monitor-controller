using System.Runtime.InteropServices;
using monitor_controller.Infrastructure;

namespace monitor_controller.Display;

public sealed class DdcController : IDisplayController
{
    private const string DllName = "dxva2.dll";

    // VCP codes
    private const byte VCP_BRIGHTNESS = 0x10;
    private const byte VCP_CONTRAST = 0x12;

    private bool _disposed;
    private string? _errorMessage;
    private readonly List<PhysicalMonitorEntry> _physicalMonitors = new();

    public bool IsAvailable => _errorMessage == null;
    public string? ErrorMessage => _errorMessage;

    #region P/Invoke Declarations

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(
        POINT pt,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        uint dwFlags);

    [DllImport(DllName, SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    // Use IntPtr for the array parameter to avoid automatic array marshalling.
    // The marshaller for [Out] struct arrays with ByValTStr fields can corrupt
    // the struct layout. We manually read the PHYSICAL_MONITOR structures from
    // a native buffer using Marshal.PtrToStructure.
    [DllImport(DllName, SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        IntPtr pPhysicalMonitorArray);

    [DllImport(DllName, SetLastError = true)]
    private static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

    [DllImport(DllName, SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(
        uint dwPhysicalMonitorArraySize,
        [In] PHYSICAL_MONITOR_NATIVE[] pPhysicalMonitorArray);

    [DllImport(DllName, SetLastError = true)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(
        IntPtr hMonitor,
        byte bVCPCode,
        out IntPtr pvct,
        out uint pdwCurrentValue,
        out uint pdwMaximumValue);

    [DllImport(DllName, SetLastError = true)]
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
    private struct POINT
    {
        public int X;
        public int Y;
    }

    // Native structure:
    // typedef struct _PHYSICAL_MONITOR {
    //     HANDLE hPhysicalMonitor;
    //     WCHAR szPhysicalMonitorDescription[128];
    // } PHYSICAL_MONITOR;
    //
    // The description is WCHAR (Unicode), so CharSet.Unicode is required.
    // Using the default CharSet.Ansi would marshal the string as 128 bytes
    // instead of 256 bytes, corrupting the struct layout and causing
    // hPhysicalMonitor to be read from the wrong offset.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PHYSICAL_MONITOR_NATIVE
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    // Managed representation used for storing monitored monitor state
    private readonly record struct PhysicalMonitorEntry(
        IntPtr PhysicalMonitorHandle,
        string Description);

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
                Logger.Info(
                    $"EnumDisplayMonitors callback: HMonitor=0x{hMonitor.ToInt64():X}");

                if (hMonitor == IntPtr.Zero)
                {
                    Logger.Error("EnumDisplayMonitors callback received a zero HMONITOR handle.");
                    return true;
                }

                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numMonitors))
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error(
                        $"GetNumberOfPhysicalMonitorsFromHMONITOR failed. HMonitor=0x{hMonitor.ToInt64():X}. Win32Error={error}");
                    return true;
                }

                Logger.Info(
                    $"GetNumberOfPhysicalMonitorsFromHMONITOR succeeded. HMonitor=0x{hMonitor.ToInt64():X}, Count={numMonitors}");

                if (numMonitors == 0)
                {
                    Logger.Warning(
                        $"GetNumberOfPhysicalMonitorsFromHMONITOR returned 0 monitors. HMonitor=0x{hMonitor.ToInt64():X}");
                    return true;
                }

                // Allocate a native buffer for the PHYSICAL_MONITOR array
                int structSize = Marshal.SizeOf<PHYSICAL_MONITOR_NATIVE>();
                IntPtr buffer = Marshal.AllocHGlobal(structSize * (int)numMonitors);
                try
                {
                    // Zero the buffer to ensure clean state
                    for (int i = 0; i < structSize * (int)numMonitors; i++)
                    {
                        Marshal.WriteByte(buffer, i, 0);
                    }

                    Logger.Info(
                        $"Calling GetPhysicalMonitorsFromHMONITOR with buffer size={structSize * numMonitors} bytes, " +
                        $"struct size={structSize} bytes, count={numMonitors}, HMonitor=0x{hMonitor.ToInt64():X}");

                    if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, numMonitors, buffer))
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Error(
                            $"GetPhysicalMonitorsFromHMONITOR failed. HMonitor=0x{hMonitor.ToInt64():X}. Win32Error={error}");
                        return true;
                    }

                    Logger.Info(
                        $"GetPhysicalMonitorsFromHMONITOR succeeded. HMonitor=0x{hMonitor.ToInt64():X}, Count={numMonitors}");

                    // Log raw bytes of the first structure for diagnosis
                    if (numMonitors >= 1)
                    {
                        var rawBytes = new byte[Math.Min(32, structSize)];
                        for (int i = 0; i < rawBytes.Length; i++)
                        {
                            rawBytes[i] = Marshal.ReadByte(buffer, i);
                        }
                        Logger.Info(
                            $"Raw first 32 bytes of PHYSICAL_MONITOR array: {BitConverter.ToString(rawBytes)}");
                    }

                    for (int i = 0; i < numMonitors; i++)
                    {
                        IntPtr structPtr = IntPtr.Add(buffer, i * structSize);
                        var physMonitor = Marshal.PtrToStructure<PHYSICAL_MONITOR_NATIVE>(structPtr);

                        Logger.Info(
                            $"Physical monitor[{i}]: Handle=0x{physMonitor.hPhysicalMonitor.ToInt64():X}, " +
                            $"Description='{physMonitor.szPhysicalMonitorDescription}'");

                        if (physMonitor.hPhysicalMonitor == IntPtr.Zero)
                        {
                            Logger.Error(
                                $"GetPhysicalMonitorsFromHMONITOR returned a zero physical monitor handle for element {i}.");
                            continue;
                        }

                        _physicalMonitors.Add(new PhysicalMonitorEntry(
                            physMonitor.hPhysicalMonitor,
                            physMonitor.szPhysicalMonitorDescription));

                        monitors.Add(new PhysicalMonitorInfo(
                            physMonitor.hPhysicalMonitor,
                            physMonitor.szPhysicalMonitorDescription,
                            new HMonitor(hMonitor))
                        {
                            Id = physMonitor.hPhysicalMonitor.ToString()
                        });

                        Logger.Info(
                            $"Monitor detected: {physMonitor.szPhysicalMonitorDescription}, Handle=0x{physMonitor.hPhysicalMonitor.ToInt64():X}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                return true;
            }

            try
            {
                Logger.Info("Enumerating physical monitors...");
                Logger.Info($"Process bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

                // Diagnostic: get primary monitor via MonitorFromPoint and test directly
                var primaryMonitor = MonitorFromPoint(new POINT { X = 0, Y = 0 }, 1 /* MONITOR_DEFAULTTONEAREST */);
                Logger.Info($"MonitorFromPoint(0,0) returned HMonitor=0x{primaryMonitor.ToInt64():X}");

                if (primaryMonitor != IntPtr.Zero)
                {
                    if (GetNumberOfPhysicalMonitorsFromHMONITOR(primaryMonitor, out uint diagCount))
                    {
                        Logger.Info($"Diagnostic GetNumberOfPhysicalMonitorsFromHMONITOR: HMonitor=0x{primaryMonitor.ToInt64():X}, Count={diagCount}");

                        if (diagCount > 0)
                        {
                            int structSize = Marshal.SizeOf<PHYSICAL_MONITOR_NATIVE>();
                            IntPtr diagBuffer = Marshal.AllocHGlobal(structSize * (int)diagCount);
                            try
                            {
                                for (int i = 0; i < structSize * (int)diagCount; i++)
                                {
                                    Marshal.WriteByte(diagBuffer, i, 0);
                                }

                                if (GetPhysicalMonitorsFromHMONITOR(primaryMonitor, diagCount, diagBuffer))
                                {
                                    for (int i = 0; i < diagCount; i++)
                                    {
                                        IntPtr structPtr = IntPtr.Add(diagBuffer, i * structSize);
                                        var diagMonitor = Marshal.PtrToStructure<PHYSICAL_MONITOR_NATIVE>(structPtr);
                                        Logger.Info(
                                            $"Diagnostic Physical monitor[{i}]: Handle=0x{diagMonitor.hPhysicalMonitor.ToInt64():X}, " +
                                            $"Description='{diagMonitor.szPhysicalMonitorDescription}'");
                                    }
                                }
                                else
                                {
                                    int error = Marshal.GetLastWin32Error();
                                    Logger.Error($"Diagnostic GetPhysicalMonitorsFromHMONITOR failed. Win32Error={error}");
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(diagBuffer);
                            }
                        }
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Error($"Diagnostic GetNumberOfPhysicalMonitorsFromHMONITOR failed. Win32Error={error}");
                    }
                }

                if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    _errorMessage = $"Failed to enumerate monitors. Win32Error={error}";
                    Logger.Error(_errorMessage);
                }
                else
                {
                    _errorMessage = null;
                    Logger.Info($"Monitor enumeration complete. Detected {monitors.Count} physical monitor(s).");
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to enumerate monitors: {ex.Message}";
                Logger.Error(_errorMessage, ex);
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
                if (monitorHandle == IntPtr.Zero)
                {
                    _errorMessage = "Cannot use zero physical monitor handle for DDC/CI.";
                    Logger.Error(_errorMessage);
                    return false;
                }

                Logger.Info(
                    $"Setting VCP 0x{vcpCode:X2} to {value}, Monitor=0x{monitorHandle.ToInt64():X}");

                if (!SetVCPFeature(monitorHandle, vcpCode, value))
                {
                    int error = Marshal.GetLastWin32Error();
                    _errorMessage = $"Failed to set VCP code 0x{vcpCode:X2}";
                    Logger.Error(
                        $"Failed to set VCP 0x{vcpCode:X2} to {value}. Win32Error={error}");
                    return false;
                }

                _errorMessage = null;
                Logger.Info(
                    $"VCP 0x{vcpCode:X2} set to {value} successfully, Monitor=0x{monitorHandle.ToInt64():X}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Exception setting VCP code 0x{vcpCode:X2}: {ex.Message}";
                Logger.Error(
                    $"Exception setting VCP 0x{vcpCode:X2} to {value}, Monitor=0x{monitorHandle.ToInt64():X}",
                    ex);
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
                if (monitorHandle == IntPtr.Zero)
                {
                    _errorMessage = "Cannot use zero physical monitor handle for DDC/CI.";
                    Logger.Error(_errorMessage);
                    return null;
                }

                Logger.Info(
                    $"Reading VCP 0x{vcpCode:X2}, Monitor=0x{monitorHandle.ToInt64():X}");

                if (!GetVCPFeatureAndVCPFeatureReply(
                    monitorHandle,
                    vcpCode,
                    out IntPtr pvct,
                    out uint currentValue,
                    out uint maxValue))
                {
                    int error = Marshal.GetLastWin32Error();
                    _errorMessage = $"Failed to get VCP code 0x{vcpCode:X2}";
                    Logger.Error(
                        $"Failed to get VCP 0x{vcpCode:X2}. Win32Error={error}");
                    return null;
                }

                _errorMessage = null;
                Logger.Info(
                    $"VCP 0x{vcpCode:X2}: Current={currentValue}, Max={maxValue}");
                return (byte)currentValue;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Exception getting VCP code 0x{vcpCode:X2}: {ex.Message}";
                Logger.Error(
                    $"Exception getting VCP 0x{vcpCode:X2}, Monitor=0x{monitorHandle.ToInt64():X}",
                    ex);
                return null;
            }
        });
    }

    private void ReleaseMonitors()
    {
        if (_physicalMonitors.Count > 0)
        {
            Logger.Info($"Releasing {_physicalMonitors.Count} physical monitor handle(s).");

            try
            {
                var monitors = _physicalMonitors
                    .Select(m => new PHYSICAL_MONITOR_NATIVE
                    {
                        hPhysicalMonitor = m.PhysicalMonitorHandle,
                        szPhysicalMonitorDescription = m.Description
                    })
                    .ToArray();

                if (!DestroyPhysicalMonitors((uint)monitors.Length, monitors))
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error(
                        $"Failed to destroy {monitors.Length} physical monitor(s). Win32Error={error}");

                    // Fall back to individual destruction
                    foreach (var monitor in _physicalMonitors)
                    {
                        try
                        {
                            if (!DestroyPhysicalMonitor(monitor.PhysicalMonitorHandle))
                            {
                                int individualError = Marshal.GetLastWin32Error();
                                Logger.Error(
                                    $"Failed to destroy physical monitor 0x{monitor.PhysicalMonitorHandle.ToInt64():X}. Win32Error={individualError}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(
                                $"Exception destroying physical monitor 0x{monitor.PhysicalMonitorHandle.ToInt64():X}",
                                ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Exception destroying {_physicalMonitors.Count} physical monitor(s).",
                    ex);

                // Fall back to individual destruction
                foreach (var monitor in _physicalMonitors)
                {
                    try
                    {
                        if (!DestroyPhysicalMonitor(monitor.PhysicalMonitorHandle))
                        {
                            int individualError = Marshal.GetLastWin32Error();
                            Logger.Error(
                                $"Failed to destroy physical monitor 0x{monitor.PhysicalMonitorHandle.ToInt64():X}. Win32Error={individualError}");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Logger.Error(
                            $"Exception destroying physical monitor 0x{monitor.PhysicalMonitorHandle.ToInt64():X}",
                            ex2);
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