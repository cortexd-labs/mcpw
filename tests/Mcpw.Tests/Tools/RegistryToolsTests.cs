using System.Text.Json;
using Mcpw.Tools;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tests.Tools;

public sealed class RegistryToolsTests
{
    private static (RegistryTools tools, Mock<IRegistryAccess> regMock) MakeTools()
    {
        var reg = new Mock<IRegistryAccess>();
        return (new RegistryTools(reg.Object), reg);
    }

    [Fact]
    public void Domain_is_registry()
    {
        var (tools, _) = MakeTools();
        tools.Domain.Should().Be("registry");
    }

    [Fact]
    public void GetTools_exposes_expected_tool_names()
    {
        var (tools, _) = MakeTools();
        var names = tools.GetTools().Select(t => t.Name).ToList();
        names.Should().Contain("registry.get");
        names.Should().Contain("registry.set");
        names.Should().Contain("registry.delete");
        names.Should().Contain("registry.list");
    }

    [Fact]
    public async Task RegistryGet_calls_registry_GetValue()
    {
        var (tools, reg) = MakeTools();
        reg.Setup(r => r.GetValue("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName"))
           .Returns("Windows 11 Pro");

        var args   = JsonSerializer.Deserialize<JsonElement>(
            """{"hive":"HKLM","key":"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion","name":"ProductName"}""");
        var result = await tools.CallAsync("registry.get", args);

        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Windows 11 Pro");
    }

    [Fact]
    public async Task RegistryGet_returns_error_when_arguments_missing()
    {
        var (tools, _) = MakeTools();
        var result = await tools.CallAsync("registry.get", null);
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrySet_calls_registry_SetValue()
    {
        var (tools, reg) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"hive":"HKCU","key":"SOFTWARE\\Test","name":"MyVal","value":"hello","kind":"String"}""");

        await tools.CallAsync("registry.set", args);

        reg.Verify(r => r.SetValue("HKCU", @"SOFTWARE\Test", "MyVal", "hello", "String"), Times.Once);
    }

    [Fact]
    public async Task RegistryDelete_value_calls_DeleteValue()
    {
        var (tools, reg) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"hive":"HKCU","key":"SOFTWARE\\Test","name":"MyVal"}""");

        await tools.CallAsync("registry.delete", args);

        reg.Verify(r => r.DeleteValue("HKCU", @"SOFTWARE\Test", "MyVal"), Times.Once);
    }

    [Fact]
    public async Task RegistryDelete_key_calls_DeleteKey()
    {
        var (tools, reg) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"hive":"HKCU","key":"SOFTWARE\\Test","recursive":true}""");

        await tools.CallAsync("registry.delete", args);

        reg.Verify(r => r.DeleteKey("HKCU", @"SOFTWARE\Test", true), Times.Once);
    }

    [Fact]
    public async Task RegistryList_returns_key_listing()
    {
        var (tools, reg) = MakeTools();
        reg.Setup(r => r.ListKey("HKLM", @"SOFTWARE"))
           .Returns(new RegistryKeyListing
           {
               Path    = @"HKLM\SOFTWARE",
               Subkeys = ["Microsoft", "Policies"],
               Values  = [],
           });

        var args   = JsonSerializer.Deserialize<JsonElement>("""{"hive":"HKLM","key":"SOFTWARE"}""");
        var result = await tools.CallAsync("registry.list", args);

        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Microsoft");
    }

    [Fact]
    public void RegistrySet_has_Dangerous_privilege_tier()
    {
        var (tools, _) = MakeTools();
        tools.GetTools().First(t => t.Name == "registry.set").Tier.Should().Be(PrivilegeTier.Dangerous);
    }

    [Fact]
    public void RegistryGet_has_Read_privilege_tier()
    {
        var (tools, _) = MakeTools();
        tools.GetTools().First(t => t.Name == "registry.get").Tier.Should().Be(PrivilegeTier.Read);
    }
}
