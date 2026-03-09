using System.Text.Json;
using Mcpw.Tools;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tests.Tools;

public sealed class ServiceToolsTests
{
    private static (ServiceTools tools, Mock<IServiceControl> svcMock, Mock<IEventLogAccess> logMock)
        MakeTools()
    {
        var svc = new Mock<IServiceControl>();
        var log = new Mock<IEventLogAccess>();
        svc.Setup(s => s.GetServices()).Returns([
            new ServiceInfo { Name = "W32Time", DisplayName = "Windows Time", Status = "Running", StartType = "Auto" },
            new ServiceInfo { Name = "WinRM",   DisplayName = "WinRM",        Status = "Stopped", StartType = "Manual" },
        ]);
        svc.Setup(s => s.GetService(It.IsAny<string>()))
           .Returns<string>(n => new ServiceInfo { Name = n, Status = "Running" });
        log.Setup(l => l.GetEntries(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
           .Returns([]);
        return (new ServiceTools(svc.Object, log.Object), svc, log);
    }

    [Fact]
    public void Domain_is_service()
    {
        var (tools, _, _) = MakeTools();
        tools.Domain.Should().Be("service");
    }

    [Fact]
    public void GetTools_exposes_expected_tool_names()
    {
        var (tools, _, _) = MakeTools();
        var names = tools.GetTools().Select(t => t.Name).ToList();
        names.Should().Contain("service.list");
        names.Should().Contain("service.start");
        names.Should().Contain("service.stop");
        names.Should().Contain("service.restart");
        names.Should().Contain("service.logs");
        names.Should().Contain("service.enable");
    }

    [Fact]
    public async Task ServiceList_returns_all_services()
    {
        var (tools, _, _) = MakeTools();
        var result = await tools.CallAsync("service.list", null);
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("W32Time");
        result.Content[0].Text.Should().Contain("WinRM");
    }

    [Fact]
    public async Task ServiceStatus_returns_info_for_named_service()
    {
        var (tools, _, _) = MakeTools();
        var args   = JsonSerializer.Deserialize<JsonElement>("""{"name":"W32Time"}""");
        var result = await tools.CallAsync("service.status", args);
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("W32Time");
    }

    [Fact]
    public async Task ServiceStatus_returns_error_when_name_missing()
    {
        var (tools, _, _) = MakeTools();
        var result = await tools.CallAsync("service.status", null);
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task ServiceStart_calls_svc_Start()
    {
        var (tools, svc, _) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"WinRM"}""");
        var result = await tools.CallAsync("service.start", args);
        result.IsError.Should().BeFalse();
        svc.Verify(s => s.Start("WinRM"), Times.Once);
    }

    [Fact]
    public async Task ServiceStop_calls_svc_Stop()
    {
        var (tools, svc, _) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"WinRM"}""");
        await tools.CallAsync("service.stop", args);
        svc.Verify(s => s.Stop("WinRM"), Times.Once);
    }

    [Fact]
    public async Task ServiceRestart_calls_svc_Restart()
    {
        var (tools, svc, _) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"W32Time"}""");
        await tools.CallAsync("service.restart", args);
        svc.Verify(s => s.Restart("W32Time"), Times.Once);
    }

    [Fact]
    public async Task ServiceEnable_calls_SetStartType()
    {
        var (tools, svc, _) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"WinRM","start_type":"auto"}""");
        var result = await tools.CallAsync("service.enable", args);
        result.IsError.Should().BeFalse();
        svc.Verify(s => s.SetStartType("WinRM", "auto"), Times.Once);
    }

    [Fact]
    public async Task ServiceStart_rejects_injection_in_name()
    {
        var (tools, _, _) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"evil;cmd"}""");
        var result = await tools.CallAsync("service.start", args);
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ServiceStart_has_Operate_privilege_tier()
    {
        var (tools, _, _) = MakeTools();
        tools.GetTools().First(t => t.Name == "service.start").Tier.Should().Be(PrivilegeTier.Operate);
    }
}
