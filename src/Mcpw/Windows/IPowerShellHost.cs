namespace Mcpw.Windows;

/// <summary>
/// Runs constrained PowerShell commands for operations with no direct .NET API.
/// All scripts execute in Constrained Language Mode.
/// </summary>
public interface IPowerShellHost
{
    /// <summary>Run a PowerShell script and return its text output.</summary>
    Task<string> RunAsync(string script, CancellationToken ct = default);

    /// <summary>Run a PowerShell script and return structured objects (JSON-serialized).</summary>
    Task<string> RunJsonAsync(string script, CancellationToken ct = default);
}
