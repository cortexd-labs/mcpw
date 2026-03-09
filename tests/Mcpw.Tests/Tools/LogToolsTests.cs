using System.Text.Json;
using Mcpw.Tools;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tests.Tools;

public sealed class LogToolsTests
{
    private static readonly LogEntry FakeEntry = new()
    {
        TimeGenerated = "2025-01-01T00:00:00Z",
        Level         = "Error",
        Source        = "Service Control Manager",
        EventId       = 7000,
        Message       = "The WinRM service failed to start",
        LogName       = "System",
    };

    private static (LogTools tools, Mock<IEventLogAccess> logMock) MakeTools(
        IEnumerable<LogEntry>? entries = null)
    {
        var log = new Mock<IEventLogAccess>();
        log.Setup(l => l.GetEntries(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
           .Returns(entries ?? [FakeEntry]);
        log.Setup(l => l.Search(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>()))
           .Returns(entries ?? [FakeEntry]);
        log.Setup(l => l.GetLogNames())
           .Returns(["System", "Application", "Security"]);
        return (new LogTools(log.Object), log);
    }

    [Fact]
    public void Domain_is_log() => MakeTools().tools.Domain.Should().Be("log");

    [Fact]
    public void GetTools_exposes_expected_names()
    {
        var names = MakeTools().tools.GetTools().Select(t => t.Name).ToList();
        names.Should().Contain("log.tail");
        names.Should().Contain("log.search");
        names.Should().Contain("log.units");
    }

    [Fact]
    public async Task LogTail_returns_entries()
    {
        var (tools, _) = MakeTools();
        var result = await tools.CallAsync("log.tail", null);
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("WinRM");
    }

    [Fact]
    public async Task LogTail_passes_log_name_and_count_to_access()
    {
        var (tools, log) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"log_name":"Application","count":25}""");
        await tools.CallAsync("log.tail", args);
        log.Verify(l => l.GetEntries("Application", 25, null), Times.Once);
    }

    [Fact]
    public async Task LogSearch_passes_search_params_to_access()
    {
        var (tools, log) = MakeTools();
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"log_name":"System","keyword":"WinRM","level":"Error"}""");
        await tools.CallAsync("log.search", args);
        log.Verify(l => l.Search("System", "WinRM", "Error", null), Times.Once);
    }

    [Fact]
    public async Task LogUnits_returns_log_names()
    {
        var (tools, _) = MakeTools();
        var result = await tools.CallAsync("log.units", null);
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("System");
        result.Content[0].Text.Should().Contain("Application");
    }
}
