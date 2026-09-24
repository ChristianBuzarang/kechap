namespace kechap.Services;

public enum TimerMode
{
    Pomodoro,
    ShortBreak,
    LongBreak
}

/// <summary>
/// Holds the timer state for one browser session, so the countdown keeps
/// running while you navigate between pages.
/// </summary>
public sealed class PomodoroTimerService : IDisposable
{
    public const int SessionsBeforeLongBreak = 4;

    private readonly TimerSettingsService _settings;
    private readonly object _lock = new();
    private System.Threading.Timer? _ticker;
    private DateTime _endsAtUtc;

    public PomodoroTimerService(TimerSettingsService settings)
    {
        _settings = settings;
        SecondsLeft = DurationFor(TimerMode.Pomodoro);
    }

    /// <summary>Raised whenever the displayed state changes.</summary>
    public event Action? Changed;

    /// <summary>Raised when a session reaches 00:00 (the mode that just finished).</summary>
    public event Action<TimerMode>? SessionCompleted;

    public TimerMode Mode { get; private set; } = TimerMode.Pomodoro;
    public int SecondsLeft { get; private set; }
    public bool IsRunning { get; private set; }
    public int CompletedPomodoros { get; private set; }
    public int FocusedMinutes { get; private set; }

    public string FormattedTime => $"{SecondsLeft / 60:00}:{SecondsLeft % 60:00}";

    public void Toggle()
    {
        lock (_lock)
        {
            if (IsRunning) StopTicker();
            else StartTicker();
        }
        Changed?.Invoke();
    }

    public void SetMode(TimerMode mode)
    {
        lock (_lock)
        {
            StopTicker();
            Mode = mode;
            SecondsLeft = DurationFor(mode);
        }
        Changed?.Invoke();
    }

    /// <summary>Call after the settings change so an idle timer shows the new duration.</summary>
    public void ApplySettings()
    {
        lock (_lock)
        {
            if (!IsRunning) SecondsLeft = DurationFor(Mode);
        }
        Changed?.Invoke();
    }

    private int DurationFor(TimerMode mode) => mode switch
    {
        TimerMode.ShortBreak => _settings.ShortBreakMinutes * 60,
        TimerMode.LongBreak => _settings.LongBreakMinutes * 60,
        _ => _settings.PomodoroMinutes * 60
    };

    // Must be called while holding _lock.
    private void StartTicker()
    {
        IsRunning = true;
        _endsAtUtc = DateTime.UtcNow.AddSeconds(SecondsLeft);
        _ticker?.Dispose();
        _ticker = new System.Threading.Timer(Tick, null, 250, 250);
    }

    // Must be called while holding _lock.
    private void StopTicker()
    {
        IsRunning = false;
        _ticker?.Dispose();
        _ticker = null;
    }

    private void Tick(object? state)
    {
        var completed = false;
        var changed = false;
        var finishedMode = TimerMode.Pomodoro;

        lock (_lock)
        {
            if (!IsRunning) return;

            // Based on the clock, not on counting ticks, so it never drifts.
            var remaining = (int)Math.Ceiling((_endsAtUtc - DateTime.UtcNow).TotalSeconds);

            if (remaining <= 0)
            {
                finishedMode = Mode;
                completed = true;
                FinishCurrentMode();
            }
            else if (remaining != SecondsLeft)
            {
                SecondsLeft = remaining;
                changed = true;
            }
        }

        if (completed) SessionCompleted?.Invoke(finishedMode);
        if (completed || changed) Changed?.Invoke();
    }

    // Must be called while holding _lock.
    private void FinishCurrentMode()
    {
        StopTicker();

        if (Mode == TimerMode.Pomodoro)
        {
            CompletedPomodoros++;
            FocusedMinutes += _settings.PomodoroMinutes;
            Mode = CompletedPomodoros % SessionsBeforeLongBreak == 0
                ? TimerMode.LongBreak
                : TimerMode.ShortBreak;
        }
        else
        {
            Mode = TimerMode.Pomodoro;
        }

        SecondsLeft = DurationFor(Mode);

        var autoStart = Mode == TimerMode.Pomodoro
            ? _settings.AutoStartPomodoros
            : _settings.AutoStartBreaks;

        if (autoStart) StartTicker();
    }

    public void Dispose()
    {
        lock (_lock) StopTicker();
    }
}