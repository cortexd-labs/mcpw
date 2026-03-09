using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Mcpw.Types;

namespace Mcpw.Tools;

public sealed class SecurityTools : IToolHandler
{
    public string Domain => "security";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("security.certs",      "List certificates from Windows certificate stores", PrivilegeTier.Read,
            """{"type":"object","properties":{"store":{"type":"string","default":"My","description":"Store name: My, Root, CA, TrustedPeople"},"location":{"type":"string","enum":["CurrentUser","LocalMachine"],"default":"LocalMachine"}}}"""),
        Tool("security.open_ports", "List TCP listening ports (alias of network.ports)",  PrivilegeTier.Read, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "security.certs"      => Certs(args),
            "security.open_ports" => OpenPorts(),
            _                     => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult Certs(JsonElement? args)
    {
        var storeName = args?.TryGetProperty("store",    out var s) == true ? s.GetString() ?? "My" : "My";
        var location  = args?.TryGetProperty("location", out var l) == true ? l.GetString() ?? "LocalMachine" : "LocalMachine";

        var storeLocation = location.Equals("CurrentUser", StringComparison.OrdinalIgnoreCase)
            ? StoreLocation.CurrentUser
            : StoreLocation.LocalMachine;

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates
            .Cast<X509Certificate2>()
            .Select(c => new CertificateInfo
            {
                Subject       = c.Subject,
                Issuer        = c.Issuer,
                Thumbprint    = c.Thumbprint,
                NotBefore     = c.NotBefore.ToString("o"),
                NotAfter      = c.NotAfter.ToString("o"),
                Store         = $"{storeLocation}/{storeName}",
                HasPrivateKey = c.HasPrivateKey,
            })
            .ToList();

        return McpJson.JsonResult(certs);
    }

    private McpCallToolResult OpenPorts()
    {
        var props     = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = props.GetActiveTcpListeners()
            .Select(ep => new TcpEndpoint { Address = ep.Address.ToString(), Port = ep.Port })
            .ToList();
        return McpJson.JsonResult(listeners);
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
