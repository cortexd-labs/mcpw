using Mcpw.Types;

namespace Mcpw.Windows;

/// <summary>
/// Abstraction over Windows Registry. Implementations use Microsoft.Win32.Registry.
/// </summary>
public interface IRegistryAccess
{
    object? GetValue(string hive, string keyPath, string valueName);
    void SetValue(string hive, string keyPath, string valueName, object value, string valueKind);
    void DeleteValue(string hive, string keyPath, string valueName);
    void DeleteKey(string hive, string keyPath, bool recursive);
    RegistryKeyListing ListKey(string hive, string keyPath);
}
