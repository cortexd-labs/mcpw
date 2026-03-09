using System.Security.AccessControl;
using System.Text.Json;
using Mcpw.Types;

namespace Mcpw.Tools;

public sealed class FileTools : IToolHandler
{
    private readonly McpwConfig _config;

    public FileTools(McpwConfig config) => _config = config;

    public string Domain => "file";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("file.read",   "Read file contents",                   PrivilegeTier.Read,
            """{"type":"object","required":["path"],"properties":{"path":{"type":"string"},"encoding":{"type":"string","default":"utf-8"}}}"""),
        Tool("file.write",  "Write file contents",                  PrivilegeTier.Operate,
            """{"type":"object","required":["path","content"],"properties":{"path":{"type":"string"},"content":{"type":"string"},"encoding":{"type":"string","default":"utf-8"}}}"""),
        Tool("file.info",   "File/directory metadata and ACL owner",PrivilegeTier.Read,
            """{"type":"object","required":["path"],"properties":{"path":{"type":"string"}}}"""),
        Tool("file.search", "Search for files by name pattern",     PrivilegeTier.Read,
            """{"type":"object","required":["root","pattern"],"properties":{"root":{"type":"string"},"pattern":{"type":"string"},"recursive":{"type":"boolean","default":true}}}"""),
        Tool("file.mkdir",  "Create a directory",                   PrivilegeTier.Operate,
            """{"type":"object","required":["path"],"properties":{"path":{"type":"string"}}}"""),
        Tool("file.tail",   "Return last N lines of a file",        PrivilegeTier.Read,
            """{"type":"object","required":["path"],"properties":{"path":{"type":"string"},"lines":{"type":"integer","default":50}}}"""),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "file.read"   => ReadFile(args),
            "file.write"  => WriteFile(args),
            "file.info"   => FileInfo_(args),
            "file.search" => SearchFiles(args),
            "file.mkdir"  => MkDir(args),
            "file.tail"   => TailFile(args),
            _             => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult ReadFile(JsonElement? args)
    {
        var path = RequiredString(args, "path");
        if (path is null) return McpJson.ErrorResult("Missing required argument: path");
        var safe = InputValidator.SanitizePath(path, _config.AllowedPaths);
        return McpJson.TextResult(File.ReadAllText(safe));
    }

    private McpCallToolResult WriteFile(JsonElement? args)
    {
        var path    = RequiredString(args, "path");
        var content = RequiredString(args, "content");
        if (path is null || content is null) return McpJson.ErrorResult("Missing required arguments: path, content");
        var safe = InputValidator.SanitizePath(path, _config.AllowedPaths);
        File.WriteAllText(safe, content);
        return McpJson.TextResult($"Written {content.Length} chars to '{safe}'.");
    }

    private McpCallToolResult FileInfo_(JsonElement? args)
    {
        var path = RequiredString(args, "path");
        if (path is null) return McpJson.ErrorResult("Missing required argument: path");
        var safe = InputValidator.SanitizePath(path, _config.AllowedPaths);

        var attr = File.GetAttributes(safe);
        var isDir = attr.HasFlag(FileAttributes.Directory);

        string owner = "";
        try
        {
            var sec = isDir
                ? new DirectoryInfo(safe).GetAccessControl()
                : (FileSystemSecurity)new FileInfo(safe).GetAccessControl();
            owner = sec.GetOwner(typeof(System.Security.Principal.NTAccount))?.ToString() ?? "";
        }
        catch { /* access denied */ }

        FileMetadata meta;
        if (isDir)
        {
            var di = new DirectoryInfo(safe);
            meta = new FileMetadata
            {
                Path        = safe,
                IsDirectory = true,
                Created     = di.CreationTimeUtc.ToString("o"),
                Modified    = di.LastWriteTimeUtc.ToString("o"),
                Accessed    = di.LastAccessTimeUtc.ToString("o"),
                Attributes  = attr.ToString(),
                Owner       = owner,
            };
        }
        else
        {
            var fi = new FileInfo(safe);
            meta = new FileMetadata
            {
                Path       = safe,
                SizeBytes  = fi.Length,
                Created    = fi.CreationTimeUtc.ToString("o"),
                Modified   = fi.LastWriteTimeUtc.ToString("o"),
                Accessed   = fi.LastAccessTimeUtc.ToString("o"),
                Attributes = attr.ToString(),
                Owner      = owner,
            };
        }

        return McpJson.JsonResult(meta);
    }

    private McpCallToolResult SearchFiles(JsonElement? args)
    {
        var root      = RequiredString(args, "root");
        var pattern   = RequiredString(args, "pattern");
        if (root is null || pattern is null) return McpJson.ErrorResult("Missing required arguments: root, pattern");
        var safeRoot  = InputValidator.SanitizePath(root, _config.AllowedPaths);
        var recursive = args?.TryGetProperty("recursive", out var r) == true ? r.GetBoolean() : true;
        var option    = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files     = Directory.EnumerateFiles(safeRoot, pattern, option).Take(500).ToList();
        return McpJson.JsonResult(files);
    }

    private McpCallToolResult MkDir(JsonElement? args)
    {
        var path = RequiredString(args, "path");
        if (path is null) return McpJson.ErrorResult("Missing required argument: path");
        var safe = InputValidator.SanitizePath(path, _config.AllowedPaths);
        Directory.CreateDirectory(safe);
        return McpJson.TextResult($"Directory '{safe}' created.");
    }

    private McpCallToolResult TailFile(JsonElement? args)
    {
        var path  = RequiredString(args, "path");
        if (path is null) return McpJson.ErrorResult("Missing required argument: path");
        var safe  = InputValidator.SanitizePath(path, _config.AllowedPaths);
        var lines = args?.TryGetProperty("lines", out var l) == true ? l.GetInt32() : 50;
        var all   = File.ReadAllLines(safe);
        var tail  = all.TakeLast(lines);
        return McpJson.TextResult(string.Join("\n", tail));
    }

    private static string? RequiredString(JsonElement? args, string key) =>
        args?.TryGetProperty(key, out var v) == true ? v.GetString() : null;

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
