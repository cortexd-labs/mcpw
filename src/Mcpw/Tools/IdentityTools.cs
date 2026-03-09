using System.Security.Principal;
using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class IdentityTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public IdentityTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "identity";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("identity.users",  "List local user accounts",           PrivilegeTier.Read, "{}"),
        Tool("identity.groups", "List local groups",                  PrivilegeTier.Read, "{}"),
        Tool("identity.whoami", "Current process identity and groups",PrivilegeTier.Read, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "identity.users"  => Users(),
            "identity.groups" => Groups(),
            "identity.whoami" => WhoAmI_(),
            _                 => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult Users()
    {
        var users = _wmi.Query("SELECT Name, FullName, Description, Disabled, LocalAccount, SID FROM Win32_UserAccount WHERE LocalAccount=True")
            .Select(r => new LocalUser
            {
                Name         = r["Name"]?.ToString()         ?? "",
                FullName     = r["FullName"]?.ToString()     ?? "",
                Description  = r["Description"]?.ToString()  ?? "",
                Enabled      = !(bool)(r["Disabled"] ?? true),
                LocalAccount = (bool)(r["LocalAccount"] ?? true),
                Sid          = r["SID"]?.ToString()          ?? "",
            })
            .ToList();
        return McpJson.JsonResult(users);
    }

    private McpCallToolResult Groups()
    {
        var groups = _wmi.Query("SELECT Name, Description, SID FROM Win32_Group WHERE LocalAccount=True")
            .Select(r => new LocalGroup
            {
                Name        = r["Name"]?.ToString()        ?? "",
                Description = r["Description"]?.ToString() ?? "",
                Sid         = r["SID"]?.ToString()         ?? "",
            })
            .ToList();
        return McpJson.JsonResult(groups);
    }

    private McpCallToolResult WhoAmI_()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        var info = new WhoAmI
        {
            Name               = identity.Name,
            Sid                = identity.User?.ToString() ?? "",
            AuthenticationType = identity.AuthenticationType ?? "",
            IsSystem           = identity.IsSystem,
            Groups             = identity.Groups?
                .Select(g => { try { return g.Translate(typeof(NTAccount)).ToString(); } catch { return g.ToString(); } })
                .ToList() ?? [],
        };
        return McpJson.JsonResult(info);
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
