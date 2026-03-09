using System.Text.Json;
using Mcpw;

namespace Mcpw.Tests;

public sealed class ConfigTests
{
    // ── Defaults ──────────────────────────────────────────────────────────

    [Fact]
    public void Default_config_has_expected_enabled_domains()
    {
        var config = new McpwConfig();
        config.EnabledDomains.Should().Contain("system");
        config.EnabledDomains.Should().Contain("registry");
        config.EnabledDomains.Should().NotContain("ad");
    }

    [Fact]
    public void Default_config_allows_all_non_disabled_domains()
    {
        var config = new McpwConfig();
        config.IsDomainEnabled("system").Should().BeTrue();
        config.IsDomainEnabled("process").Should().BeTrue();
    }

    // ── IsDomainEnabled logic ─────────────────────────────────────────────

    [Fact]
    public void IsDomainEnabled_returns_false_for_disabled_domain()
    {
        var config = new McpwConfig
        {
            EnabledDomains  = ["system", "process"],
            DisabledDomains = ["process"],
        };
        config.IsDomainEnabled("process").Should().BeFalse();
    }

    [Fact]
    public void IsDomainEnabled_returns_false_for_domain_not_in_enabledList()
    {
        var config = new McpwConfig
        {
            EnabledDomains = ["system"],
        };
        config.IsDomainEnabled("registry").Should().BeFalse();
    }

    [Fact]
    public void IsDomainEnabled_is_case_insensitive()
    {
        var config = new McpwConfig
        {
            EnabledDomains  = ["System"],
            DisabledDomains = ["REGISTRY"],
        };
        config.IsDomainEnabled("system").Should().BeTrue();
        config.IsDomainEnabled("registry").Should().BeFalse();
    }

    [Fact]
    public void IsDomainEnabled_returns_true_when_enabledList_is_empty()
    {
        var config = new McpwConfig { EnabledDomains = [] };
        config.IsDomainEnabled("anything").Should().BeTrue();
    }

    // ── ConfigLoader.Load ─────────────────────────────────────────────────

    [Fact]
    public void Load_returns_default_config_when_file_not_found()
    {
        var config = ConfigLoader.Load(@"C:\nonexistent\path\config.json");
        config.Should().NotBeNull();
        config.EnabledDomains.Should().NotBeEmpty();
    }

    [Fact]
    public void Load_parses_json_config_correctly()
    {
        var json = """
            {
                "allowedPaths": ["C:\\Users", "C:\\inetpub"],
                "enabledDomains": ["system", "iis"],
                "disabledDomains": [],
                "privilegeTier": "operate"
            }
            """;

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            var config = ConfigLoader.Load(path);
            config.AllowedPaths.Should().Contain(@"C:\Users");
            config.EnabledDomains.Should().Contain("iis");
            config.PrivilegeTier.Should().Be("operate");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_handles_malformed_json_gracefully()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ invalid json }}}");
            var act = () => ConfigLoader.Load(path);
            act.Should().Throw<JsonException>();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
