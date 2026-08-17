using System.Text.Json;
using monitor_controller.Scheduling;

namespace monitor_controller.Configuration;

public sealed class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorController",
        "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return await SaveDefaultAsync();
            }

            var json = await File.ReadAllTextAsync(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            if (config == null || config.Profiles.Count == 0)
            {
                return await SaveDefaultAsync();
            }

            return config;
        }
        catch
        {
            return await SaveDefaultAsync();
        }
    }

    public async Task<AppConfig> SaveAsync(AppConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
            return config;
        }
        catch
        {
            return config;
        }
    }

    private static async Task<AppConfig> SaveDefaultAsync()
    {
        var defaultConfig = AppConfig.Default;
        var service = new ConfigService();
        await service.SaveAsync(defaultConfig);
        return defaultConfig;
    }
}
