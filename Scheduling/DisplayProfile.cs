using System.Text.Json.Serialization;

namespace monitor_controller.Scheduling;

public sealed record DisplayProfile(
    string Time,
    byte Brightness,
    byte Contrast)
{
    private TimeOnly? _timeOnly;

    [JsonIgnore]
    public TimeOnly TimeOnly
    {
        get
        {
            _timeOnly ??= TimeOnly.TryParse(Time, out var parsed)
                ? parsed
                : TimeOnly.MinValue;
            return _timeOnly.Value;
        }
    }
}