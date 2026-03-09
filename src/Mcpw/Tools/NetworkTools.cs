using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class NetworkTools : IToolHandler
{
    private readonly IPowerShellHost _ps;

    public NetworkTools(IPowerShellHost ps) => _ps = ps;

    public string Domain => "network";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("network.interfaces",  "List NICs with IP, MAC, status",        PrivilegeTier.Read, "{}"),
        Tool("network.ports",       "Listening TCP/UDP ports",                PrivilegeTier.Read, "{}"),
        Tool("network.connections", "Active TCP connections",                 PrivilegeTier.Read, "{}"),
        Tool("network.dns",         "DNS servers per interface",              PrivilegeTier.Read, "{}"),
        Tool("network.firewall",    "Windows Firewall rules",                 PrivilegeTier.Read,
            """{"type":"object","properties":{"direction":{"type":"string","enum":["Inbound","Outbound","All"],"default":"All"}}}"""),
        Tool("network.routing",     "IP routing table",                       PrivilegeTier.Read, "{}"),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        return toolName switch
        {
            "network.interfaces"  => Interfaces(),
            "network.ports"       => Ports(),
            "network.connections" => Connections(),
            "network.dns"         => Dns(),
            "network.firewall"    => await Firewall(args, ct),
            "network.routing"     => await Routing(ct),
            _                     => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
    }

    private McpCallToolResult Interfaces()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Select(nic =>
            {
                var props = nic.GetIPProperties();
                return new Mcpw.Types.NetworkInterface
                {
                    Name         = nic.Name,
                    Description  = nic.Description,
                    MacAddress   = nic.GetPhysicalAddress().ToString(),
                    Status       = nic.OperationalStatus.ToString(),
                    SpeedMbps    = nic.Speed / 1_000_000,
                    Ipv4Addresses = props.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString())
                        .ToList(),
                    Ipv6Addresses = props.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        .Select(a => a.Address.ToString())
                        .ToList(),
                };
            })
            .ToList();

        return McpJson.JsonResult(nics);
    }

    private McpCallToolResult Ports()
    {
        var props = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = props.GetActiveTcpListeners()
            .Select(ep => new TcpEndpoint { Address = ep.Address.ToString(), Port = ep.Port })
            .ToList();
        return McpJson.JsonResult(listeners);
    }

    private McpCallToolResult Connections()
    {
        var props = IPGlobalProperties.GetIPGlobalProperties();
        var conns = props.GetActiveTcpConnections()
            .Select(c => new TcpConnection
            {
                Local  = new TcpEndpoint { Address = c.LocalEndPoint.Address.ToString(),  Port = c.LocalEndPoint.Port },
                Remote = new TcpEndpoint { Address = c.RemoteEndPoint.Address.ToString(), Port = c.RemoteEndPoint.Port },
                State  = c.State.ToString(),
            })
            .ToList();
        return McpJson.JsonResult(conns);
    }

    private McpCallToolResult Dns()
    {
        var dns = NetworkInterface.GetAllNetworkInterfaces()
            .ToDictionary(
                nic => nic.Name,
                nic => nic.GetIPProperties().DnsAddresses.Select(a => a.ToString()).ToList());
        return McpJson.JsonResult(dns);
    }

    private async Task<McpCallToolResult> Firewall(JsonElement? args, CancellationToken ct)
    {
        var direction = args?.TryGetProperty("direction", out var d) == true
            ? d.GetString() ?? "All"
            : "All";

        var dirFilter = direction == "All" ? "" : $" | Where-Object Direction -eq '{direction}'";
        var json = await _ps.RunJsonAsync(
            $"Get-NetFirewallRule -Enabled True{dirFilter} | Select-Object -First 200 Name,Direction,Action,Protocol,Enabled", ct);
        return McpJson.TextResult(json);
    }

    private async Task<McpCallToolResult> Routing(CancellationToken ct)
    {
        var json = await _ps.RunJsonAsync(
            "Get-NetRoute | Select-Object DestinationPrefix,NextHop,InterfaceAlias,RouteMetric,Protocol", ct);
        return McpJson.TextResult(json);
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
