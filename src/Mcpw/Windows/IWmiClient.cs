namespace Mcpw.Windows;

/// <summary>
/// Abstraction over WMI/CIM queries. Implementations use System.Management.
/// Interface exists so tool classes can be unit-tested without WMI access.
/// </summary>
public interface IWmiClient
{
    /// <summary>Execute a WQL query and return each result as a property bag.</summary>
    IEnumerable<IReadOnlyDictionary<string, object?>> Query(string wql, string? scope = null);
}
