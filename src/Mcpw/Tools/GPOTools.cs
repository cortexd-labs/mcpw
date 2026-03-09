using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class GPOTools : IToolHandler
{
    private readonly IPowerShellHost _ps;

    public GPOTools(IPowerShellHost ps) => _ps = ps;

    public string Domain => "gpo";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("gpo.list",   "List applied Group Policy Objects",               PrivilegeTier.Domain, "{}"),
        Tool("gpo.result", "Resultant Set of Policy for user/computer",       PrivilegeTier.Domain,
            """{"type":"object","properties":{"user":{"type":"string"},"computer":{"type":"string"}}}"""),
        Tool("gpo.update", "Force group policy refresh (gpupdate /force)",    PrivilegeTier.Domain, "{}"),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        return toolName switch
        {
            "gpo.list"   => await GpoList(ct),
            "gpo.result" => await GpoResult(args, ct),
            "gpo.update" => await GpoUpdate(ct),
            _            => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
    }

    private async Task<McpCallToolResult> GpoList(CancellationToken ct)
    {
        var json = await _ps.RunJsonAsync(
            "Get-GPResultantSetOfPolicy -ReportType Xml -Path $env:TEMP\\rsop.xml; [xml](Get-Content $env:TEMP\\rsop.xml) | Select-Object -ExpandProperty Rsop | ConvertTo-Json -Depth 3",
            ct);
        return McpJson.TextResult(json);
    }

    private async Task<McpCallToolResult> GpoResult(JsonElement? args, CancellationToken ct)
    {
        var user     = args?.TryGetProperty("user",     out var u) == true ? u.GetString() : null;
        var computer = args?.TryGetProperty("computer", out var c) == true ? c.GetString() : null;
        if (user is not null) InputValidator.AssertNoInjection(user, "user");
        if (computer is not null) InputValidator.AssertNoInjection(computer, "computer");

        var userArg     = user     is not null ? $"-User '{EscapePs(user)}'" : "";
        var computerArg = computer is not null ? $"-Computer '{EscapePs(computer)}'" : "";
        var output = await _ps.RunAsync($"gpresult /r {userArg} {computerArg}", ct);
        return McpJson.TextResult(output);
    }

    private async Task<McpCallToolResult> GpoUpdate(CancellationToken ct)
    {
        var output = await _ps.RunAsync("gpupdate /force", ct);
        return McpJson.TextResult(output);
    }

    private static string EscapePs(string s) => s.Replace("'", "''");

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
