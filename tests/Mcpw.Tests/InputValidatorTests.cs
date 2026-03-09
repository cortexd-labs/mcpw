using Mcpw;

namespace Mcpw.Tests;

public sealed class InputValidatorTests
{
    // ── AssertNoInjection ──────────────────────────────────────────────────

    [Theory]
    [InlineData("safe-service-name")]
    [InlineData("My Service 2")]
    [InlineData("W32Time")]
    public void AssertNoInjection_accepts_clean_values(string value)
    {
        var act = () => InputValidator.AssertNoInjection(value, "param");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("name; DROP TABLE")]
    [InlineData("cmd | evil")]
    [InlineData("$env:SECRET")]
    [InlineData("payload`cmd`")]
    [InlineData("cmd\x00null")]
    public void AssertNoInjection_rejects_injection_chars(string value)
    {
        var act = () => InputValidator.AssertNoInjection(value, "param");
        act.Should().Throw<ArgumentException>();
    }

    // ── SanitizePath ──────────────────────────────────────────────────────

    [Fact]
    public void SanitizePath_rejects_path_traversal()
    {
        var act = () => InputValidator.SanitizePath(@"C:\Users\..\Windows\System32");
        act.Should().Throw<ArgumentException>().WithMessage("*traversal*");
    }

    [Fact]
    public void SanitizePath_rejects_blocked_system_paths()
    {
        var act = () => InputValidator.SanitizePath(@"C:\Windows\System32\config\SAM");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void SanitizePath_rejects_NTDS_path()
    {
        var act = () => InputValidator.SanitizePath(@"C:\Windows\NTDS\ntds.dit");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void SanitizePath_returns_full_path_for_valid_input()
    {
        // Use a known-safe path that doesn't require existence
        var result = InputValidator.SanitizePath(@"C:\Users\Public");
        result.Should().Be(@"C:\Users\Public");
    }

    [Fact]
    public void SanitizePath_rejects_empty_path()
    {
        var act = () => InputValidator.SanitizePath("");
        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    // ── SanitizePath with allowlist ────────────────────────────────────────

    [Fact]
    public void SanitizePath_with_allowlist_rejects_path_outside_allowed_prefixes()
    {
        var allowed = new List<string> { @"C:\inetpub" };
        var act     = () => InputValidator.SanitizePath(@"C:\Users\Public", allowed);
        act.Should().Throw<UnauthorizedAccessException>().WithMessage("*allowed prefix*");
    }

    [Fact]
    public void SanitizePath_with_empty_allowlist_accepts_any_valid_path()
    {
        var result = InputValidator.SanitizePath(@"C:\Users\Public", []);
        result.Should().Be(@"C:\Users\Public");
    }

    // ── ParsePid ──────────────────────────────────────────────────────────

    [Fact]
    public void ParsePid_accepts_valid_pid()
    {
        InputValidator.ParsePid("1234").Should().Be(1234);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    public void ParsePid_rejects_invalid_values(string value)
    {
        var act = () => InputValidator.ParsePid(value);
        act.Should().Throw<ArgumentException>();
    }
}
