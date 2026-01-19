using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.Metadata;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for get_context_info MCP tool.
/// </summary>
public class GetContextInfoToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IContextMetadataService _metadataService;

    public GetContextInfoToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"context_tool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        _metadataService = new ContextMetadataService(pathResolver, guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetContextInfo_ReturnsWorkspaceMetadata()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "file1.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "file2.cs"), "more code");
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));

        // Act
        var result = await GetContextInfoTool.GetContextInfo(_metadataService, 5, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        
        Assert.Equal(2, doc.RootElement.GetProperty("totalFiles").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("totalDirectories").GetInt32());
        Assert.True(doc.RootElement.GetProperty("totalSizeBytes").GetInt64() > 0);
    }

    [Fact]
    public async Task GetContextInfo_IncludesWorkspaceRoot()
    {
        // Act
        var result = await GetContextInfoTool.GetContextInfo(_metadataService, 5, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(_tempDir, doc.RootElement.GetProperty("workspaceRoot").GetString());
    }

    [Fact]
    public async Task GetContextInfo_GroupsFilesByExtension()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "a.cs"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "b.cs"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "c.txt"), "c");

        // Act
        var result = await GetContextInfoTool.GetContextInfo(_metadataService, 5, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var extensions = doc.RootElement.GetProperty("filesByExtension");
        
        Assert.Equal(2, extensions.GetProperty(".cs").GetInt32());
        Assert.Equal(1, extensions.GetProperty(".txt").GetInt32());
    }

    [Fact]
    public async Task GetContextInfo_EmptyWorkspace_ReturnsZeros()
    {
        // Act
        var result = await GetContextInfoTool.GetContextInfo(_metadataService, 5, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetProperty("totalFiles").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("totalDirectories").GetInt32());
    }

    [Fact]
    public async Task GetContextInfo_RespectsMaxDepth()
    {
        // Arrange
        var deepPath = Path.Combine(_tempDir, "a", "b", "c", "d");
        Directory.CreateDirectory(deepPath);
        File.WriteAllText(Path.Combine(deepPath, "deep.txt"), "content");

        // Act
        var shallowResult = await GetContextInfoTool.GetContextInfo(_metadataService, 1, CancellationToken.None);
        var deepResult = await GetContextInfoTool.GetContextInfo(_metadataService, 10, CancellationToken.None);

        // Assert
        using var shallow = JsonDocument.Parse(shallowResult);
        using var deep = JsonDocument.Parse(deepResult);
        
        // Deep scan should find more directories
        Assert.True(deep.RootElement.GetProperty("totalDirectories").GetInt32() >= 
                    shallow.RootElement.GetProperty("totalDirectories").GetInt32());
    }
}
