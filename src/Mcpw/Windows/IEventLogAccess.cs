using Mcpw.Types;

namespace Mcpw.Windows;

/// <summary>
/// Abstraction over Windows Event Log. Implementations use System.Diagnostics.Eventing.Reader.
/// </summary>
public interface IEventLogAccess
{
    IEnumerable<LogEntry> GetEntries(string logName, int count, string? sourceFilter = null);
    IEnumerable<LogEntry> Search(string logName, string? keyword, string? level, DateTimeOffset? since);
    IEnumerable<string> GetLogNames();
}
