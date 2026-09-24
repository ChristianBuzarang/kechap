namespace kechap.Services;

public class TimerSettingsService
{
    public static readonly string[] AlarmSounds = { "Kitchen Bell", "Digital Beep", "Bird Chirp" };

    public int PomodoroMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public bool AutoStartBreaks { get; set; }
    public bool AutoStartPomodoros { get; set; }
    public string AlarmSound { get; set; } = "Kitchen Bell";
}