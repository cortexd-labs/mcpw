using Microsoft.Win32;
using Mcpw.Types;

namespace Mcpw.Windows;

/// <summary>
/// Registry access via Microsoft.Win32.Registry.
/// Sensitive hives (SAM, SECURITY, HARDWARE\DESCRIPTION\System\CentralProcessor) are blocked.
/// </summary>
public sealed class RegistryAccess : IRegistryAccess
{
    private static readonly HashSet<string> BlockedKeyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        @"HKEY_LOCAL_MACHINE\SAM",
        @"HKEY_LOCAL_MACHINE\SECURITY",
        @"HKLM\SAM",
        @"HKLM\SECURITY",
    };

    public object? GetValue(string hive, string keyPath, string valueName)
    {
        Guard(hive, keyPath);
        using var key = OpenKey(hive, keyPath, writable: false)
            ?? throw new KeyNotFoundException($"Key not found: {hive}\\{keyPath}");
        return key.GetValue(valueName);
    }

    public void SetValue(string hive, string keyPath, string valueName, object value, string valueKind)
    {
        Guard(hive, keyPath);
        using var key = OpenKey(hive, keyPath, writable: true)
            ?? throw new KeyNotFoundException($"Key not found: {hive}\\{keyPath}");
        key.SetValue(valueName, value, ParseKind(valueKind));
    }

    public void DeleteValue(string hive, string keyPath, string valueName)
    {
        Guard(hive, keyPath);
        using var key = OpenKey(hive, keyPath, writable: true)
            ?? throw new KeyNotFoundException($"Key not found: {hive}\\{keyPath}");
        key.DeleteValue(valueName);
    }

    public void DeleteKey(string hive, string keyPath, bool recursive)
    {
        Guard(hive, keyPath);
        var root = RootKey(hive);
        if (recursive)
            root.DeleteSubKeyTree(keyPath);
        else
            root.DeleteSubKey(keyPath);
    }

    public RegistryKeyListing ListKey(string hive, string keyPath)
    {
        Guard(hive, keyPath);
        using var key = OpenKey(hive, keyPath, writable: false)
            ?? throw new KeyNotFoundException($"Key not found: {hive}\\{keyPath}");

        var subkeys = key.GetSubKeyNames().ToList();
        var values  = key.GetValueNames()
            .Select(n => new RegistryValue
            {
                Name  = n,
                Type  = key.GetValueKind(n).ToString(),
                Value = key.GetValue(n)?.ToString() ?? "",
            })
            .ToList();

        return new RegistryKeyListing
        {
            Path    = $"{hive}\\{keyPath}",
            Subkeys = subkeys,
            Values  = values,
        };
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static void Guard(string hive, string keyPath)
    {
        var full = $"{NormalizeHive(hive)}\\{keyPath}";
        foreach (var blocked in BlockedKeyPrefixes)
            if (full.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Access to registry path '{full}' is blocked.");
    }

    private static RegistryKey? OpenKey(string hive, string keyPath, bool writable) =>
        RootKey(hive).OpenSubKey(keyPath, writable);

    private static RegistryKey RootKey(string hive) => NormalizeHive(hive) switch
    {
        "HKEY_LOCAL_MACHINE"  or "HKLM" => Registry.LocalMachine,
        "HKEY_CURRENT_USER"   or "HKCU" => Registry.CurrentUser,
        "HKEY_CLASSES_ROOT"   or "HKCR" => Registry.ClassesRoot,
        "HKEY_USERS"          or "HKU"  => Registry.Users,
        "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
        _ => throw new ArgumentException($"Unknown registry hive: '{hive}'"),
    };

    private static string NormalizeHive(string hive) =>
        hive.ToUpperInvariant().Trim('\\');

    private static RegistryValueKind ParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "string"       => RegistryValueKind.String,
        "expandstring" => RegistryValueKind.ExpandString,
        "binary"       => RegistryValueKind.Binary,
        "dword"        => RegistryValueKind.DWord,
        "multistring"  => RegistryValueKind.MultiString,
        "qword"        => RegistryValueKind.QWord,
        _              => RegistryValueKind.String,
    };
}
