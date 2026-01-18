namespace RecursiveContext.Mcp.Server.Tools; 
 
/// <summary> 
/// Standard error message constants for MCP tools. 
/// </summary> 
internal static class ToolErrorMessages 
{ 
    public const string PathNotFound = "The specified path does not exist."; 
    public const string PathOutsideWorkspace = "The path is outside the workspace boundary."; 
    public const string FileTooLarge = "The file exceeds the maximum allowed size."; 
    public const string CallLimitExceeded = "Tool call limit for this session has been exceeded."; 
    public const string InvalidPattern = "The specified pattern is invalid."; 
    public const string OperationCancelled = "The operation was cancelled."; 
    public const string EmptyPath = "Path cannot be empty."; 
    public const string InvalidLineRange = "The specified line range is invalid."; 
}
