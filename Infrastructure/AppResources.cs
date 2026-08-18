using System.Reflection;

namespace monitor_controller.Infrastructure;

public static class AppResources
{
    public static Icon LoadTrayIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceName = assembly
            .GetManifestResourceNames()
            .First(x => x.EndsWith(
                "monitor-controller.ico",
                StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Resource not found: {resourceName}");

        return new Icon(stream);
    }
}