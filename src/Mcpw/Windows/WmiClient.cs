using System.Management;

namespace Mcpw.Windows;

/// <summary>
/// Production WMI client using System.Management.ManagementObjectSearcher.
/// All queries are parameterized via SelectQuery — no string interpolation in WQL.
/// </summary>
public sealed class WmiClient : IWmiClient
{
    public IEnumerable<IReadOnlyDictionary<string, object?>> Query(string wql, string? scope = null)
    {
        var mgmtScope = scope is null
            ? new ManagementScope()
            : new ManagementScope(scope);

        mgmtScope.Connect();

        using var searcher = new ManagementObjectSearcher(mgmtScope, new ObjectQuery(wql));
        using var results  = searcher.Get();

        foreach (ManagementBaseObject obj in results)
        {
            var dict = new Dictionary<string, object?>();
            foreach (PropertyData prop in obj.Properties)
                dict[prop.Name] = prop.Value;
            yield return dict;
        }
    }
}
