using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.Json;
using Mcpw.Types;

namespace Mcpw.Tools;

public sealed class ADTools : IToolHandler
{
    public string Domain => "ad";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("ad.users",         "Search Active Directory users",       PrivilegeTier.Domain,
            """{"type":"object","properties":{"filter":{"type":"string","description":"LDAP filter or display name substring"},"limit":{"type":"integer","default":100}}}"""),
        Tool("ad.groups",        "Search Active Directory groups",      PrivilegeTier.Domain,
            """{"type":"object","properties":{"filter":{"type":"string"},"limit":{"type":"integer","default":100}}}"""),
        Tool("ad.user.info",     "Detailed attributes for one user",    PrivilegeTier.Domain,
            """{"type":"object","required":["sam"],"properties":{"sam":{"type":"string","description":"sAMAccountName"}}}"""),
        Tool("ad.user.groups",   "Groups a user is member of",         PrivilegeTier.Domain,
            """{"type":"object","required":["sam"],"properties":{"sam":{"type":"string"}}}"""),
        Tool("ad.group.members", "Members of an AD group",             PrivilegeTier.Domain,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("ad.computers",     "List domain computers",               PrivilegeTier.Domain,
            """{"type":"object","properties":{"filter":{"type":"string"},"limit":{"type":"integer","default":100}}}"""),
        Tool("ad.ou.list",       "List organizational units",           PrivilegeTier.Domain, "{}"),
        Tool("ad.user.enable",   "Enable an AD user account",           PrivilegeTier.Domain,
            """{"type":"object","required":["sam"],"properties":{"sam":{"type":"string"}}}"""),
        Tool("ad.user.disable",  "Disable an AD user account",          PrivilegeTier.Domain,
            """{"type":"object","required":["sam"],"properties":{"sam":{"type":"string"}}}"""),
        Tool("ad.user.unlock",   "Unlock a locked AD user account",     PrivilegeTier.Domain,
            """{"type":"object","required":["sam"],"properties":{"sam":{"type":"string"}}}"""),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        return toolName switch
        {
            "ad.users"         => await LdapSearch("(&(objectClass=user)(objectCategory=person))", ["sAMAccountName", "displayName", "mail", "distinguishedName", "userAccountControl", "department", "title"], args, ct),
            "ad.groups"        => await LdapSearch("(objectClass=group)", ["name", "distinguishedName", "description", "groupType"], args, ct),
            "ad.user.info"     => await UserInfo(args, ct),
            "ad.user.groups"   => await UserGroups(args, ct),
            "ad.group.members" => await GroupMembers(args, ct),
            "ad.computers"     => await LdapSearch("(objectClass=computer)", ["name", "distinguishedName", "dNSHostName", "operatingSystem"], args, ct),
            "ad.ou.list"       => await LdapSearch("(objectClass=organizationalUnit)", ["name", "distinguishedName"], null, ct),
            "ad.user.enable"   => await SetUserFlag(args, enable: true, ct),
            "ad.user.disable"  => await SetUserFlag(args, enable: false, ct),
            "ad.user.unlock"   => await UnlockUser(args, ct),
            _                  => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static Task<McpCallToolResult> LdapSearch(
        string baseFilter, string[] attributes, JsonElement? args, CancellationToken ct)
    {
        var userFilter = args?.TryGetProperty("filter", out var f) == true ? f.GetString() : null;
        var limit      = args?.TryGetProperty("limit",  out var l) == true ? l.GetInt32()  : 100;

        var filter = userFilter is null
            ? baseFilter
            : $"(&{baseFilter}(|(name=*{EscapeLdap(userFilter)}*)(displayName=*{EscapeLdap(userFilter)}*)))";

        using var conn = DomainConnection();
        var request = new SearchRequest("", filter, SearchScope.Subtree, attributes)
        {
            SizeLimit = limit,
        };
        var response = (SearchResponse)conn.SendRequest(request);

        var results = response.Entries.Cast<SearchResultEntry>()
            .Select(e => e.Attributes.AttributeNames
                .Cast<string>()
                .ToDictionary(a => a, a => e.Attributes[a][0]?.ToString() ?? ""))
            .ToList();

        return Task.FromResult(McpJson.JsonResult(results));
    }

    private static Task<McpCallToolResult> UserInfo(JsonElement? args, CancellationToken ct)
    {
        var sam = args?.TryGetProperty("sam", out var s) == true ? s.GetString() : null;
        if (sam is null) return Task.FromResult(McpJson.ErrorResult("Missing required argument: sam"));
        InputValidator.AssertNoInjection(sam, "sam");

        using var conn = DomainConnection();
        var request  = new SearchRequest("", $"(sAMAccountName={EscapeLdap(sam)})", SearchScope.Subtree);
        var response = (SearchResponse)conn.SendRequest(request);
        var entry    = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();
        if (entry is null) return Task.FromResult(McpJson.ErrorResult($"User '{sam}' not found"));

        var attrs = entry.Attributes.AttributeNames
            .Cast<string>()
            .ToDictionary(a => a, a => entry.Attributes[a][0]?.ToString() ?? "");
        return Task.FromResult(McpJson.JsonResult(attrs));
    }

    private static Task<McpCallToolResult> UserGroups(JsonElement? args, CancellationToken ct)
    {
        var sam = args?.TryGetProperty("sam", out var s) == true ? s.GetString() : null;
        if (sam is null) return Task.FromResult(McpJson.ErrorResult("Missing required argument: sam"));
        InputValidator.AssertNoInjection(sam, "sam");

        using var conn = DomainConnection();
        var request  = new SearchRequest("", $"(sAMAccountName={EscapeLdap(sam)})", SearchScope.Subtree, "memberOf");
        var response = (SearchResponse)conn.SendRequest(request);
        var entry    = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();

        var groups = entry?.Attributes["memberOf"]?.GetValues(typeof(string)).Cast<string>().ToList() ?? [];
        return Task.FromResult(McpJson.JsonResult(groups));
    }

    private static Task<McpCallToolResult> GroupMembers(JsonElement? args, CancellationToken ct)
    {
        var name = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;
        if (name is null) return Task.FromResult(McpJson.ErrorResult("Missing required argument: name"));
        InputValidator.AssertNoInjection(name, "name");

        using var conn = DomainConnection();
        var request  = new SearchRequest("", $"(name={EscapeLdap(name)})", SearchScope.Subtree, "member");
        var response = (SearchResponse)conn.SendRequest(request);
        var entry    = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();

        var members = entry?.Attributes["member"]?.GetValues(typeof(string)).Cast<string>().ToList() ?? [];
        return Task.FromResult(McpJson.JsonResult(members));
    }

    private static Task<McpCallToolResult> SetUserFlag(JsonElement? args, bool enable, CancellationToken ct)
    {
        var sam = args?.TryGetProperty("sam", out var s) == true ? s.GetString() : null;
        if (sam is null) return Task.FromResult(McpJson.ErrorResult("Missing required argument: sam"));
        // userAccountControl manipulation via ModifyRequest
        // 0x0002 = ACCOUNTDISABLE bit
        using var conn = DomainConnection();
        var search   = (SearchResponse)conn.SendRequest(
            new SearchRequest("", $"(sAMAccountName={EscapeLdap(sam)})", SearchScope.Subtree, "userAccountControl", "distinguishedName"));
        var entry = search.Entries.Cast<SearchResultEntry>().FirstOrDefault();
        if (entry is null) return Task.FromResult(McpJson.ErrorResult($"User '{sam}' not found"));

        var dn      = entry.DistinguishedName;
        var current = int.Parse(entry.Attributes["userAccountControl"][0]?.ToString() ?? "512");
        var newVal  = enable ? (current & ~0x0002) : (current | 0x0002);

        var mod = new ModifyRequest(dn, DirectoryAttributeOperation.Replace, "userAccountControl", newVal.ToString());
        conn.SendRequest(mod);

        return Task.FromResult(McpJson.TextResult($"User '{sam}' {(enable ? "enabled" : "disabled")}."));
    }

    private static Task<McpCallToolResult> UnlockUser(JsonElement? args, CancellationToken ct)
    {
        var sam = args?.TryGetProperty("sam", out var s) == true ? s.GetString() : null;
        if (sam is null) return Task.FromResult(McpJson.ErrorResult("Missing required argument: sam"));
        InputValidator.AssertNoInjection(sam, "sam");

        using var conn = DomainConnection();
        var search = (SearchResponse)conn.SendRequest(
            new SearchRequest("", $"(sAMAccountName={EscapeLdap(sam)})", SearchScope.Subtree, "distinguishedName"));
        var entry = search.Entries.Cast<SearchResultEntry>().FirstOrDefault();
        if (entry is null) return Task.FromResult(McpJson.ErrorResult($"User '{sam}' not found"));

        var mod = new ModifyRequest(entry.DistinguishedName,
            DirectoryAttributeOperation.Replace, "lockoutTime", "0");
        conn.SendRequest(mod);

        return Task.FromResult(McpJson.TextResult($"User '{sam}' unlocked."));
    }

    private static LdapConnection DomainConnection()
    {
        var domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
        var conn = new LdapConnection(domainName)
        {
            AuthType = AuthType.Negotiate,
        };
        conn.Bind();
        return conn;
    }

    private static string EscapeLdap(string s) =>
        s.Replace("\\", "\\5c").Replace("*", "\\2a").Replace("(", "\\28")
         .Replace(")", "\\29").Replace("\0", "\\00");
}
