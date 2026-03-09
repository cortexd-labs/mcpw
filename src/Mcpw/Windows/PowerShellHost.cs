using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

namespace Mcpw.Windows;

/// <summary>
/// Embedded PowerShell runtime host.
/// All scripts run in Constrained Language Mode to prevent abuse.
/// </summary>
public sealed class PowerShellHost : IPowerShellHost
{
    public async Task<string> RunAsync(string script, CancellationToken ct = default)
    {
        using var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        runspace.SessionStateProxy.LanguageMode = PSLanguageMode.ConstrainedLanguage;

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddScript(script);

        var result = await Task.Run(() => ps.Invoke(), ct);

        if (ps.HadErrors)
        {
            var errors = string.Join("\n", ps.Streams.Error.Select(e => e.ToString()));
            throw new InvalidOperationException($"PowerShell error: {errors}");
        }

        return string.Join("\n", result.Select(r => r?.ToString() ?? ""));
    }

    public async Task<string> RunJsonAsync(string script, CancellationToken ct = default)
    {
        var jsonScript = $"({script}) | ConvertTo-Json -Depth 5 -Compress";
        return await RunAsync(jsonScript, ct);
    }
}
