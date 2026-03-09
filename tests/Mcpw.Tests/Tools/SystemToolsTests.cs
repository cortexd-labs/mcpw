using System.Text.Json;
using Mcpw.Tools;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tests.Tools;

public sealed class SystemToolsTests
{
    private static SystemTools MakeTools(Action<Mock<IWmiClient>>? setup = null)
    {
        var wmi = new Mock<IWmiClient>();
        wmi.Setup(w => w.Query(It.IsAny<string>(), It.IsAny<string?>()))
           .Returns([]);
        setup?.Invoke(wmi);
        return new SystemTools(wmi.Object);
    }

    [Fact]
    public void GetTools_exposes_expected_tool_names()
    {
        var tools = MakeTools().GetTools().Select(t => t.Name).ToList();
        tools.Should().Contain("system.info");
        tools.Should().Contain("system.uptime");
        tools.Should().Contain("system.env");
        tools.Should().Contain("system.reboot");
    }

    [Fact]
    public void Domain_is_system()
    {
        MakeTools().Domain.Should().Be("system");
    }

    [Fact]
    public async Task SystemInfo_returns_non_empty_text()
    {
        var wmi = new Mock<IWmiClient>();
        wmi.Setup(w => w.Query(It.IsAny<string>(), It.IsAny<string?>()))
           .Returns([new Dictionary<string, object?>
           {
               ["CSName"]          = "TESTHOST",
               ["Caption"]         = "Windows 11 Pro",
               ["Version"]         = "10.0.22621",
               ["OSArchitecture"]  = "64-bit",
               ["NumberOfProcessors"] = 8,
               ["LastBootUpTime"]  = "",
               ["Domain"]          = "WORKGROUP",
           }]);

        var tools  = new SystemTools(wmi.Object);
        var result = await tools.CallAsync("system.info", null);

        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("TESTHOST");
        result.Content[0].Text.Should().Contain("Windows 11 Pro");
    }

    [Fact]
    public async Task Uptime_returns_numeric_string()
    {
        var result = await MakeTools().CallAsync("system.uptime", null);
        result.IsError.Should().BeFalse();
        long.TryParse(result.Content[0].Text, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Env_returns_list_of_variables()
    {
        var result = await MakeTools().CallAsync("system.env", null);
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("PATH");
    }

    [Fact]
    public async Task Env_filters_by_name_argument()
    {
        var args   = JsonSerializer.Deserialize<JsonElement>("""{"name":"PATH"}""");
        var result = await MakeTools().CallAsync("system.env", args);
        result.IsError.Should().BeFalse();
        var text = result.Content[0].Text;
        text.Should().Contain("PATH");
    }

    [Fact]
    public async Task UnknownTool_returns_error()
    {
        var result = await MakeTools().CallAsync("system.nonexistent", null);
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void SystemReboot_has_Dangerous_privilege_tier()
    {
        var reboot = MakeTools().GetTools().First(t => t.Name == "system.reboot");
        reboot.Tier.Should().Be(PrivilegeTier.Dangerous);
    }

    [Fact]
    public void SystemInfo_has_Read_privilege_tier()
    {
        var info = MakeTools().GetTools().First(t => t.Name == "system.info");
        info.Tier.Should().Be(PrivilegeTier.Read);
    }
}
