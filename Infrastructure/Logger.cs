using System.Text;

namespace monitor_controller.Infrastructure;

/// <summary>
/// Simple thread-safe file logger for diagnostic purposes.
/// Writes to %LOCALAPPDATA%\MonitorController\logs\monitor-controller.log
/// Never throws exceptions back to the application.
/// </summary>
public static class Logger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorController",
        "logs");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "monitor-controller.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(string message, Exception ex) =>
        Write("ERROR", $"{message}{Environment.NewLine}{ex}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }
}