namespace Mcpw;

/// <summary>
/// Central input validation. Rejects command injection characters,
/// path traversal, and access to blocked paths.
/// </summary>
public static class InputValidator
{
    // Characters that could enable injection in WQL, shell, or LDAP contexts
    private static readonly char[] InjectionChars = ['`', ';', '|', '&', '$', '\0', '\r', '\n'];

    private static readonly string[] PathTraversalParts = ["..", "..\\", "../"];

    private static readonly HashSet<string> BlockedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        @"C:\Windows\System32\config",
        @"C:\Windows\NTDS",
        @"C:\Windows\System32\winevt\Logs\Security.evtx",
    };

    /// <summary>Throws if the string contains injection-prone characters.</summary>
    public static void AssertNoInjection(string value, string paramName)
    {
        if (value.IndexOfAny(InjectionChars) >= 0)
            throw new ArgumentException(
                $"Parameter '{paramName}' contains disallowed characters.", paramName);
    }

    /// <summary>
    /// Validates and canonicalizes a file-system path.
    /// Throws on path traversal or blocked prefixes.
    /// </summary>
    public static string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.");

        // Block traversal sequences before full expansion
        foreach (var part in PathTraversalParts)
            if (path.Contains(part, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Path traversal detected in: '{path}'");

        var full = Path.GetFullPath(path);

        foreach (var blocked in BlockedPathPrefixes)
            if (full.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Access to path '{full}' is blocked.");

        return full;
    }

    /// <summary>Validates a path against a caller-supplied allowlist.</summary>
    public static string SanitizePath(string path, IReadOnlyList<string> allowedPrefixes)
    {
        var full = SanitizePath(path);

        if (allowedPrefixes.Count > 0 &&
            !allowedPrefixes.Any(p => full.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException(
                $"Path '{full}' is not under an allowed prefix.");
        }

        return full;
    }

    /// <summary>Throws if value is not a valid process identifier.</summary>
    public static int ParsePid(string value)
    {
        if (!int.TryParse(value, out var pid) || pid <= 0)
            throw new ArgumentException($"Invalid process ID: '{value}'");
        return pid;
    }
}
