using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

/// <summary>
/// IIS management via Microsoft.Web.Administration (requires IIS to be installed).
/// The assembly is part of IIS and not available as a NuGet package — reference via GAC on Windows.
/// Loaded via reflection to avoid hard dependency at startup.
/// </summary>
public sealed class IISTools : IToolHandler
{
    private readonly IPowerShellHost _ps;

    public IISTools(IPowerShellHost ps) => _ps = ps;

    public string Domain => "iis";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("iis.sites",        "List IIS websites",                    PrivilegeTier.Read,    "{}"),
        Tool("iis.pools",        "List IIS application pools",           PrivilegeTier.Read,    "{}"),
        Tool("iis.site.start",   "Start an IIS website",                 PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("iis.site.stop",    "Stop an IIS website",                  PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("iis.pool.recycle", "Recycle an IIS application pool",      PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        return toolName switch
        {
            "iis.sites"        => await Sites(ct),
            "iis.pools"        => await Pools(ct),
            "iis.site.start"   => await SiteOp(args, "Start", ct),
            "iis.site.stop"    => await SiteOp(args, "Stop",  ct),
            "iis.pool.recycle" => await PoolRecycle(args, ct),
            _                  => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
    }

    private async Task<McpCallToolResult> Sites(CancellationToken ct)
    {
        var json = await _ps.RunJsonAsync(
            "Import-Module WebAdministration; Get-Website | Select-Object Name,ID,State,PhysicalPath,@{N='Bindings';E={($_.Bindings.Collection | ForEach-Object {$_.protocol+'://'+$_.bindingInformation}) -join ','}}", ct);
        return McpJson.TextResult(json);
    }

    private async Task<McpCallToolResult> Pools(CancellationToken ct)
    {
        var json = await _ps.RunJsonAsync(
            "Import-Module WebAdministration; Get-WebConfiguration system.applicationHost/applicationPools/add | Select-Object Name,State,AutoStart,ManagedRuntimeVersion,@{N='PipelineMode';E={$_.ManagedPipelineMode}},@{N='IdentityType';E={$_.ProcessModel.userName}}", ct);
        return McpJson.TextResult(json);
    }

    private async Task<McpCallToolResult> SiteOp(JsonElement? args, string op, CancellationToken ct)
    {
        var name = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");
        await _ps.RunAsync($"Import-Module WebAdministration; {op}-Website -Name '{EscapePs(name)}'", ct);
        return McpJson.TextResult($"IIS site '{name}': {op} completed.");
    }

    private async Task<McpCallToolResult> PoolRecycle(JsonElement? args, CancellationToken ct)
    {
        var name = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");
        await _ps.RunAsync($"Import-Module WebAdministration; Restart-WebAppPool -Name '{EscapePs(name)}'", ct);
        return McpJson.TextResult($"App pool '{name}' recycled.");
    }

    private static string EscapePs(string s) => s.Replace("'", "''");

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
