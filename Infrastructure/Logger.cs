using System.Text;

namespace monitor_controller.Infrastructure;

/// <summary>
/// Simple thread-safe file logger for diagnostic purposes.
/// Writes to %LOCALAPPDATA%\MonitorController\logs\monitor-controller.log
/// Rotates the log file when it exceeds 3 MB, keeping a maximum of 3 rotated files.
/// Never throws exceptions back to the application.
/// </summary>
public static class Logger
{
    private const long MaxLogFileSizeBytes = 3 * 1024 * 1024; // 3 MB
    private const int MaxRotatedFiles = 3;

    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Monitor Controller",
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

                // Rotate the log file if it exceeds the maximum size
                RotateIfNeeded();

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the application.
        }
    }

    private static void RotateIfNeeded()
    {
        var fileInfo = new FileInfo(LogFilePath);
        if (!fileInfo.Exists || fileInfo.Length < MaxLogFileSizeBytes)
        {
            return;
        }

        // Delete the oldest rotated file if it exists
        var oldestFile = Path.Combine(LogDirectory, $"monitor-controller.log.{MaxRotatedFiles}");
        if (File.Exists(oldestFile))
        {
            File.Delete(oldestFile);
        }

        // Shift rotated files: .2 -> .3, .1 -> .2, etc.
        for (int i = MaxRotatedFiles - 1; i >= 1; i--)
        {
            var source = Path.Combine(LogDirectory, $"monitor-controller.log.{i}");
            var destination = Path.Combine(LogDirectory, $"monitor-controller.log.{i + 1}");
            if (File.Exists(source))
            {
                File.Move(source, destination, overwrite: true);
            }
        }

        // Rotate the current log file to .1
        File.Move(LogFilePath, Path.Combine(LogDirectory, "monitor-controller.log.1"), overwrite: true);
    }
}