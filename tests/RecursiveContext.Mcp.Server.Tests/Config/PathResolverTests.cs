using RecursiveContext.Mcp.Server.Config;

namespace RecursiveContext.Mcp.Server.Tests.Config;

public class PathResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _resolver;

    public PathResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"path_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _resolver = new PathResolver(settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void WorkspaceRoot_ReturnsConfiguredPath()
    {
        Assert.Equal(_tempDir, _resolver.WorkspaceRoot);
    }

    [Fact]
    public void ResolvePath_ValidRelativePath_ReturnsFullPath()
    {
        var result = _resolver.ResolvePath("subdir/file.txt");

        Assert.True(result.IsSuccess);
        Assert.StartsWith(_tempDir, result.Value);
    }

    [Fact]
    public void ResolvePath_EmptyPath_ReturnsFailure()
    {
        var result = _resolver.ResolvePath("");

        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.Error);
    }

    [Fact]
    public void ResolvePath_WhitespacePath_ReturnsFailure()
    {
        var result = _resolver.ResolvePath("   ");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ResolvePath_DirectoryTraversal_ReturnsFailure()
    {
        var result = _resolver.ResolvePath("../../../etc/passwd");

        Assert.True(result.IsFailure);
        Assert.Contains("outside workspace", result.Error);
    }

    [Fact]
    public void ResolvePath_AbsolutePathOutsideWorkspace_ReturnsFailure()
    {
        var result = _resolver.ResolvePath("C:\\Windows\\System32");

        Assert.True(result.IsFailure);
        Assert.Contains("outside workspace", result.Error);
    }

    [Fact]
    public void ResolvePath_AbsolutePathInsideWorkspace_ReturnsSuccess()
    {
        var insidePath = Path.Combine(_tempDir, "inside.txt");

        var result = _resolver.ResolvePath(insidePath);

        Assert.True(result.IsSuccess);
        Assert.Equal(insidePath, result.Value);
    }

    [Fact]
    public void ResolvePath_NormalizesForwardSlashes()
    {
        var result = _resolver.ResolvePath("sub/dir/file.txt");

        Assert.True(result.IsSuccess);
        var expected = Path.Combine(_tempDir, "sub", "dir", "file.txt");
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ResolvePath_NormalizesBackslashes()
    {
        var result = _resolver.ResolvePath("sub\\dir\\file.txt");

        Assert.True(result.IsSuccess);
        var expected = Path.Combine(_tempDir, "sub", "dir", "file.txt");
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ResolveAndValidateExists_ExistingDirectory_ReturnsSuccess()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "existing"));

        var result = _resolver.ResolveAndValidateExists("existing");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ResolveAndValidateExists_ExistingFile_ReturnsSuccess()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "content");

        var result = _resolver.ResolveAndValidateExists("file.txt");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ResolveAndValidateExists_NonexistentPath_ReturnsFailure()
    {
        var result = _resolver.ResolveAndValidateExists("does_not_exist.txt");

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public void ResolveAndValidateExists_TraversalAttempt_ReturnsFailure()
    {
        var result = _resolver.ResolveAndValidateExists("../../etc/passwd");

        Assert.True(result.IsFailure);
        Assert.Contains("outside workspace", result.Error);
    }

    [Fact]
    public void ResolvePath_DotPath_ResolvesToWorkspaceRoot()
    {
        var result = _resolver.ResolvePath(".");

        Assert.True(result.IsSuccess);
        Assert.Equal(_tempDir, result.Value);
    }


    [Fact]
    public void Constructor_RelativeWorkspaceRoot_ResolvesToAbsolutePath()
    {
        // Simulates RLM_WORKSPACE_ROOT="."
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var resolver = new PathResolver(settings);

        // Should resolve to current directory as absolute path
        Assert.True(Path.IsPathRooted(resolver.WorkspaceRoot));
        Assert.Equal(Path.GetFullPath("."), resolver.WorkspaceRoot);
    }

    [Fact]
    public void Constructor_RelativeSubdirectoryWorkspaceRoot_ResolvesToAbsolutePath()
    {
        // Simulates RLM_WORKSPACE_ROOT="./src"
        var settings = new RlmSettings("./src", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var resolver = new PathResolver(settings);

        // Should resolve to absolute path
        Assert.True(Path.IsPathRooted(resolver.WorkspaceRoot));
        Assert.Equal(Path.GetFullPath("./src"), resolver.WorkspaceRoot);
    }


    #region ToRelativePath Tests

    [Fact]
    public void ToRelativePath_DotPath_ReturnsDot()
    {
        var result = _resolver.ToRelativePath(".");

        Assert.True(result.IsSuccess);
        Assert.Equal(".", result.Value);
    }

    [Fact]
    public void ToRelativePath_EmptyPath_ReturnsFailure()
    {
        var result = _resolver.ToRelativePath("");

        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.Error);
    }

    [Fact]
    public void ToRelativePath_WorkspaceRootAbsolute_ReturnsDot()
    {
        var result = _resolver.ToRelativePath(_tempDir);

        Assert.True(result.IsSuccess);
        Assert.Equal(".", result.Value);
    }

    [Fact]
    public void ToRelativePath_WorkspaceRootWithTrailingSlash_ReturnsDot()
    {
        var result = _resolver.ToRelativePath(_tempDir + Path.DirectorySeparatorChar);

        Assert.True(result.IsSuccess);
        Assert.Equal(".", result.Value);
    }

    [Fact]
    public void ToRelativePath_AbsoluteSubPath_ReturnsRelative()
    {
        var absolutePath = Path.Combine(_tempDir, "sub", "dir");

        var result = _resolver.ToRelativePath(absolutePath);

        Assert.True(result.IsSuccess);
        Assert.Equal("sub/dir", result.Value);
    }

    [Fact]
    public void ToRelativePath_AbsoluteOutsideWorkspace_ReturnsFailure()
    {
        var result = _resolver.ToRelativePath("C:\\Windows\\System32");

        Assert.True(result.IsFailure);
        Assert.Contains("outside workspace", result.Error);
    }

    [Fact]
    public void ToRelativePath_RelativePath_ReturnsNormalized()
    {
        var result = _resolver.ToRelativePath("sub\\dir\\file.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal("sub/dir/file.txt", result.Value);
    }

    [Fact]
    public void ToRelativePath_RelativeWithForwardSlashes_ReturnsNormalized()
    {
        var result = _resolver.ToRelativePath("sub/dir/file.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal("sub/dir/file.txt", result.Value);
    }

    #endregion
}
