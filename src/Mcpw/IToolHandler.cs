using System.Text.Json;
using Mcpw.Types;

namespace Mcpw;

/// <summary>
/// Implemented by each tool domain class.
/// Server.cs discovers and routes to these via DI.
/// </summary>
public interface IToolHandler
{
    /// <summary>Domain prefix, e.g. "system", "process", "registry".</summary>
    string Domain { get; }

    /// <summary>All tools this handler exposes.</summary>
    IEnumerable<McpToolDefinition> GetTools();

    /// <summary>Execute a specific tool by full name (e.g. "system.info").</summary>
    Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default);
}
