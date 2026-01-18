using System.Text.Json; 
using System.Text.Json.Serialization; 
using CSharpFunctionalExtensions; 
 
namespace RecursiveContext.Mcp.Server.Tools; 
 
/// <summary> 
/// Static helpers for formatting MCP tool responses. 
/// </summary> 
internal static class ToolResponseFormatter 
{ 
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        WriteIndented = true, 
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
    }; 
 
    public static string FormatSuccess<T>(T data) 
    { 
        return JsonSerializer.Serialize(data, JsonOptions); 
    } 
 
    public static string FormatError(string error) 
    { 
        return JsonSerializer.Serialize(new { Error = error }, JsonOptions); 
    } 
 
    public static string FormatResult<T>(Result<T> result) 
    { 
        return result.IsSuccess 
            ? FormatSuccess(result.Value) 
            : FormatError(result.Error); 
    } 
}
