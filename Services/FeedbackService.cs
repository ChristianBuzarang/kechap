using System.Text.Json;
using kechap.Models;

namespace kechap.Services;

/// <summary>
/// Stores ratings and comments in App_Data/feedback.json (no database yet).
/// Registered as a singleton, so every browser tab sees the same list and
/// the data survives page reloads and app restarts.
/// </summary>
public sealed class FeedbackService
{
    public const int MaxNameLength = 40;
    public const int MaxCommentLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly List<FeedbackEntry> _entries;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(IWebHostEnvironment env, ILogger<FeedbackService> logger)
    {
        _logger = logger;

        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "feedback.json");

        _entries = Load();
    }

    /// <summary>Raised after a new review is saved, so open pages can refresh.</summary>
    public event Action? Changed;

    /// <summary>All reviews, newest first.</summary>
    public IReadOnlyList<FeedbackEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.OrderByDescending(e => e.CreatedAtUtc).ToList();
        }
    }

    public FeedbackEntry Add(string name, int rating, string comment)
    {
        var entry = new FeedbackEntry
        {
            Name = Truncate(name.Trim(), MaxNameLength),
            Rating = Math.Clamp(rating, 1, 5),
            Comment = Truncate(comment.Trim(), MaxCommentLength),
            CreatedAtUtc = DateTime.UtcNow
        };

        lock (_lock)
        {
            _entries.Add(entry);
            Save();
        }

        Changed?.Invoke();
        return entry;
    }

    private List<FeedbackEntry> Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<FeedbackEntry>>(json);
                if (loaded is not null)
                {
                    // Skip entries with an out-of-range rating so a hand-edited file cannot break the page.
                    return loaded.Where(e => e.Rating is >= 1 and <= 5).ToList();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read {Path}; starting with the sample comments.", _filePath);
        }

        // First run: start with the two sample comments from feedback.html.
        var seed = new List<FeedbackEntry>
        {
            new()
            {
                Name = "Sarah Jenkins",
                Rating = 5,
                Comment = "Absolute lifesaver during my finals week! The aesthetic design makes studying feel so much cozier.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Name = "Liam Davis",
                Rating = 4,
                Comment = "Super clean and minimal Pomodoro app. Would love an option to customize custom alarm sound themes later!",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        };

        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(seed, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not write {Path}.", _filePath);
        }

        return seed;
    }

    // Must be called while holding _lock.
    private void Save()
    {
        // Write to a temp file first, then swap it in, so a crash mid-write
        // can never leave a half-written feedback.json behind.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(_entries, JsonOptions));
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}