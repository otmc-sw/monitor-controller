namespace monitor_controller.Scheduling;

public record DisplayProfile(
    string Time,
    byte Brightness,
    byte Contrast
)
{
    public TimeOnly TimeOnly { get; } = TimeOnly.Parse(Time);
}
