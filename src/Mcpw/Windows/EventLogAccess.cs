using System.Diagnostics.Eventing.Reader;
using Mcpw.Types;

namespace Mcpw.Windows;

/// <summary>
/// Reads Windows Event Log using System.Diagnostics.Eventing.Reader (EvtQuery).
/// </summary>
public sealed class EventLogAccess : IEventLogAccess
{
    public IEnumerable<LogEntry> GetEntries(string logName, int count, string? sourceFilter = null)
    {
        var sourceClause = sourceFilter is null
            ? ""
            : $" and Source='{EscapeXml(sourceFilter)}'";

        var query = new EventLogQuery(logName, PathType.LogName,
            $"*[System{sourceClause}]")
        {
            ReverseDirection = true,
        };

        using var reader = new EventLogReader(query);
        var read = 0;
        EventRecord? record;
        while (read < count && (record = reader.ReadEvent()) is not null)
        {
            using (record)
            {
                yield return Map(record, logName);
                read++;
            }
        }
    }

    public IEnumerable<LogEntry> Search(string logName, string? keyword, string? level, DateTimeOffset? since)
    {
        var conditions = new List<string>();
        if (level is not null)
            conditions.Add($"Level={LevelCode(level)}");
        if (since.HasValue)
            conditions.Add($"TimeCreated[@SystemTime>='{since.Value.UtcDateTime:o}']");

        var systemFilter = conditions.Count > 0
            ? $"*[System[{string.Join(" and ", conditions)}]]"
            : "*";

        var query = new EventLogQuery(logName, PathType.LogName, systemFilter)
        {
            ReverseDirection = true,
        };

        using var reader = new EventLogReader(query);
        EventRecord? record;
        while ((record = reader.ReadEvent()) is not null)
        {
            using (record)
            {
                var entry = Map(record, logName);
                if (keyword is null || entry.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    yield return entry;
            }
        }
    }

    public IEnumerable<string> GetLogNames()
    {
        using var session = new EventLogSession();
        return session.GetLogNames().ToList();
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static LogEntry Map(EventRecord r, string logName) => new()
    {
        TimeGenerated = r.TimeCreated?.ToString("o") ?? "",
        Level         = LevelName(r.Level),
        Source        = r.ProviderName ?? "",
        EventId       = r.Id,
        Message       = SafeMessage(r),
        LogName       = logName,
    };

    private static string SafeMessage(EventRecord r)
    {
        try { return r.FormatDescription() ?? ""; }
        catch { return $"Event ID {r.Id}"; }
    }

    private static string LevelName(byte? level) => level switch
    {
        1 => "Critical",
        2 => "Error",
        3 => "Warning",
        4 => "Information",
        5 => "Verbose",
        _ => "Unknown",
    };

    private static string LevelCode(string level) => level.ToLowerInvariant() switch
    {
        "critical"    => "1",
        "error"       => "2",
        "warning"     => "3",
        "information" => "4",
        "verbose"     => "5",
        _             => "4",
    };

    private static string EscapeXml(string s) =>
        s.Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;")
         .Replace("<", "&lt;").Replace(">", "&gt;");
}
