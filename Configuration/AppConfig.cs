using monitor_controller.Scheduling;

namespace monitor_controller.Configuration;

public record AppConfig(
    List<DisplayProfile> Profiles,
    string? SelectedMonitorHandle,
    bool IsFirstRun = true,
    bool IsSchedulerEnabled = true
)
{
    public static AppConfig Default => new(
        new List<DisplayProfile>
        {
            // Sáng
            new DisplayProfile("04:00", 50, 15),
            new DisplayProfile("05:00", 55, 20),
            new DisplayProfile("06:00", 60, 25),
            new DisplayProfile("07:00", 65, 30),
            new DisplayProfile("08:00", 70, 35),
            // Chiều / Tối
            new DisplayProfile("16:00", 70, 35),
            new DisplayProfile("17:00", 65, 30),
            new DisplayProfile("18:00", 60, 25),
            new DisplayProfile("19:00", 55, 20),
            new DisplayProfile("20:00", 50, 15)
        },
        null
    );
}
