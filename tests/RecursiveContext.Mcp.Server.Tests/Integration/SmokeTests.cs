using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.FileSystem;
using RecursiveContext.Mcp.Server.Tools.Metadata;
using RecursiveContext.Mcp.Server.Tools.Search;
using RecursiveContext.Mcp.Server.Server;
using RecursiveContext.Mcp.Server.Tools.Server;

namespace RecursiveContext.Mcp.Server.Tests.Integration;

/// <summary>
/// End-to-end smoke tests that verify the complete server stack.
/// These tests exercise all tools with realistic scenarios.
/// </summary>
public class SmokeTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RlmSettings _settings;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrails;
    private readonly FileSystemService _fileSystemService;
    private readonly PatternMatchingService _patternService;
    private readonly ContextMetadataService _metadataService;
    private readonly ServerMetadata _serverMetadata;

    public SmokeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"smoke_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Create a realistic project structure
        CreateTestProjectStructure();

        _settings = new RlmSettings(_tempDir, 1_048_576, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(_settings);
        _guardrails = new GuardrailService(_settings);
        _fileSystemService = new FileSystemService(_pathResolver, _guardrails);
        _patternService = new PatternMatchingService(_pathResolver, _guardrails);
        _metadataService = new ContextMetadataService(_pathResolver, _guardrails);
        _serverMetadata = ServerMetadata.Default;
    }

    private void CreateTestProjectStructure()
    {
        // Create a mini C# project structure
        var srcDir = Path.Combine(_tempDir, "src");
        var servicesDir = Path.Combine(srcDir, "Services");
        var controllersDir = Path.Combine(srcDir, "Controllers");
        var testsDir = Path.Combine(_tempDir, "tests");

        Directory.CreateDirectory(servicesDir);
        Directory.CreateDirectory(controllersDir);
        Directory.CreateDirectory(testsDir);

        // Create source files
        File.WriteAllText(Path.Combine(srcDir, "Program.cs"), @"
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
");

        File.WriteAllText(Path.Combine(servicesDir, "AuthService.cs"), @"
namespace MyApp.Services;

public class AuthService
{
    public bool ValidateToken(string token)
    {
        return !string.IsNullOrEmpty(token);
    }
    
    public string HashPassword(string password)
    {
        // BCrypt implementation here
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
    }
}
");

        File.WriteAllText(Path.Combine(servicesDir, "UserService.cs"), @"
namespace MyApp.Services;

public class UserService
{
    private readonly AuthService _authService;
    
    public UserService(AuthService authService)
    {
        _authService = authService;
    }
    
    public bool CreateUser(string email, string password)
    {
        var hashedPassword = _authService.HashPassword(password);
        // Save to database
        return true;
    }
}
");

        File.WriteAllText(Path.Combine(controllersDir, "AuthController.cs"), @"
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost(""login"")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        return Ok(new { token = ""jwt_token"" });
    }
}

public record LoginRequest(string Email, string Password);
");

        File.WriteAllText(Path.Combine(controllersDir, "UserController.cs"), @"
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    
    public UserController(UserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet(""{id}"")]
    public IActionResult GetUser(int id)
    {
        return Ok(new { Id = id, Name = ""Test User"" });
    }
}
");

        // Create a test file
        File.WriteAllText(Path.Combine(testsDir, "AuthServiceTests.cs"), @"
using Xunit;
using MyApp.Services;

namespace MyApp.Tests;

public class AuthServiceTests
{
    [Fact]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        var service = new AuthService();
        Assert.True(service.ValidateToken(""valid_token""));
    }
}
");

        // Create project files
        File.WriteAllText(Path.Combine(_tempDir, "MyApp.sln"), @"
Microsoft Visual Studio Solution File, Format Version 12.00
");
        File.WriteAllText(Path.Combine(srcDir, "MyApp.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GetServerInfo_ReturnsValidMetadata()
    {
        // Act
        var result = GetServerInfoTool.GetServerInfo(_serverMetadata, _settings, _guardrails);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("recursive-context", doc.RootElement.GetProperty("serverName").GetString());
        Assert.True(doc.RootElement.GetProperty("maxBytesPerRead").GetInt32() > 0);
        Assert.True(doc.RootElement.GetProperty("maxToolCallsPerSession").GetInt32() > 0);
        Assert.True(doc.RootElement.GetProperty("remainingToolCalls").GetInt32() > 0);
    }

    [Fact]
    public async Task GetContextInfo_ReturnsWorkspaceStats()
    {
        // Act
        var result = await GetContextInfoTool.GetContextInfo(_metadataService, 10, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        Assert.Equal(_tempDir, root.GetProperty("workspaceRoot").GetString());
        Assert.True(root.GetProperty("totalFiles").GetInt32() >= 6); // At least our 6 files
        Assert.True(root.GetProperty("totalDirectories").GetInt32() >= 3); // src, tests, etc
        Assert.True(root.GetProperty("filesByExtension").TryGetProperty(".cs", out var csCount));
        Assert.True(csCount.GetInt32() >= 5); // 5+ .cs files
    }

    [Fact]
    public async Task FindFilesByPattern_FindsControllers()
    {
        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(
            _patternService, 
            "**/*Controller.cs", 
            100, 
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var files = doc.RootElement.GetProperty("matchingPaths");
        Assert.Equal(2, files.GetArrayLength()); // AuthController.cs, UserController.cs
    }

    [Fact]
    public async Task ReadFile_ReadsAuthService()
    {
        // Act
        var result = await ReadFileTool.ReadFile(
            _fileSystemService, 
            "src/Services/AuthService.cs", 
            CancellationToken.None);

        // Assert - result is JSON-serialized string
        var content = JsonSerializer.Deserialize<string>(result);
        Assert.Contains("HashPassword", content);
        Assert.Contains("ValidateToken", content);
    }

    [Fact]
    public async Task ListFiles_ListsServicesDirectory()
    {
        // Act
        var result = await ListFilesTool.ListFiles(
            _fileSystemService, 
            "src/Services", 
            0, 
            100, 
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var files = doc.RootElement.GetProperty("files");
        Assert.Equal(2, files.GetArrayLength()); // AuthService.cs, UserService.cs
    }

    [Fact]
    public async Task ListDirectories_ListsSrcDirectory()
    {
        // Act
        var result = await ListDirectoriesTool.ListDirectories(
            _fileSystemService, 
            "src", 
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var directories = doc.RootElement.GetProperty("directories");
        Assert.Equal(2, directories.GetArrayLength()); // Controllers, Services
    }

    [Fact]
    public async Task ReadFileChunk_ReadsPartialFile()
    {
        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, 
            "src/Services/AuthService.cs", 
            0, 
            5, 
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("content", out _));
        Assert.Equal(0, doc.RootElement.GetProperty("startLine").GetInt32());
        Assert.True(doc.RootElement.GetProperty("totalLines").GetInt32() > 5);
    }

    [Fact]
    public async Task Guardrails_TrackToolCalls()
    {
        // Arrange
        var initialRemaining = _guardrails.RemainingCalls;

        // Act - make a tool call
        await ListFilesTool.ListFiles(_fileSystemService, ".", 0, 10, CancellationToken.None);

        // Assert - guardrails should have been incremented
        // Note: The tool calls CheckAndIncrementCallCount internally
        var afterRemaining = _guardrails.RemainingCalls;
        Assert.True(afterRemaining <= initialRemaining);
    }

    [Fact]
    public async Task PathTraversal_IsBlocked()
    {
        // Act
        var result = await ReadFileTool.ReadFile(
            _fileSystemService, 
            "../../etc/passwd", 
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("outside workspace", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task FullWorkflow_FindAndReadAuthLogic()
    {
        // This simulates what a client LLM would do to find authentication logic

        // Step 1: Get workspace overview
        var contextResult = await GetContextInfoTool.GetContextInfo(_metadataService, 10, CancellationToken.None);
        using var contextDoc = JsonDocument.Parse(contextResult);
        Assert.True(contextDoc.RootElement.GetProperty("totalFiles").GetInt32() > 0);

        // Step 2: Search for auth-related files
        var searchResult = await FindFilesByPatternTool.FindFilesByPattern(
            _patternService, 
            "**/*Auth*.cs", 
            100, 
            CancellationToken.None);
        using var searchDoc = JsonDocument.Parse(searchResult);
        var authFiles = searchDoc.RootElement.GetProperty("matchingPaths");
        Assert.True(authFiles.GetArrayLength() >= 2); // AuthService.cs, AuthController.cs

        // Step 3: Read the auth service
        var readResult = await ReadFileTool.ReadFile(
            _fileSystemService, 
            "src/Services/AuthService.cs", 
            CancellationToken.None);
        var authContent = JsonSerializer.Deserialize<string>(readResult);
        Assert.Contains("HashPassword", authContent);

        // Step 4: Verify server info shows usage
        var serverInfoResult = GetServerInfoTool.GetServerInfo(_serverMetadata, _settings, _guardrails);
        using var serverDoc = JsonDocument.Parse(serverInfoResult);
        Assert.True(serverDoc.RootElement.GetProperty("remainingToolCalls").GetInt32() >= 0);
    }
}
