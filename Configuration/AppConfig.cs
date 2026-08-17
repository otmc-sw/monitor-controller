using monitor_controller.Scheduling;

namespace monitor_controller.Configuration;

public record AppConfig(
    List<DisplayProfile> Profiles,
    string? SelectedMonitorHandle
)
{
    public static AppConfig Default => new(
        new List<DisplayProfile>
        {
            new DisplayProfile("06:00", 60, 30),
            new DisplayProfile("08:00", 80, 50),
            new DisplayProfile("18:00", 60, 40),
            new DisplayProfile("21:00", 40, 35),
            new DisplayProfile("23:00", 25, 30)
        },
        null
    );
}
