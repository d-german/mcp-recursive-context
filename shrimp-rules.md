# Development Guidelines

## Project Overview

- **Name**: RecursiveContext.Mcp - MCP Server for Recursive Language Model reasoning
- **Tech Stack**: .NET 9.0, C# latest, ModelContextProtocol 0.5.0-preview.1, CSharpFunctionalExtensions 3.4.0
- **Purpose**: Read-only file exploration tools enabling LLMs to reason over large codebases

## Project Architecture

| Directory | Purpose |
|-----------|---------|
| `src/RecursiveContext.Mcp.Server/Config/` | Configuration (RlmSettings, PathResolver) |
| `src/RecursiveContext.Mcp.Server/Models/` | Immutable domain records |
| `src/RecursiveContext.Mcp.Server/Services/` | Business logic, interfaces in Interfaces.cs |
| `src/RecursiveContext.Mcp.Server/Tools/` | MCP tool implementations |
| `src/RecursiveContext.Mcp.Server/Server/` | Host, DI, logging |
| `tests/RecursiveContext.Mcp.Server.Tests/` | xUnit tests |

## Code Standards

### Naming
- PascalCase: types, methods, properties
- camelCase: local variables, parameters
- Prefix `I` for interfaces
- Suffix `Async` for async methods
- Suffix `Service` for services
- Suffix `Tool` for tool classes

### Patterns
- **Result<T>**: Use for operations that can fail
- **Immutability**: Use `readonly record struct` or `record`
- **Collections**: Use `IReadOnlyList<T>` or `ImmutableArray<T>`
- **Async**: Always accept `CancellationToken`, use `.ConfigureAwait(false)`

## MCP Tool Implementation

### Required Pattern
```csharp
[McpServerToolType]
internal static class ExampleTool
{
    [McpServerTool(Name = "tool_name")]
    [Description("Tool description")]
    public static async Task<string> Execute(
        IService service,
        [Description("Param description")] string param,
        CancellationToken ct = default)
    {
        var result = await service.DoWorkAsync(param, ct).ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
```

### Rules
- Tools MUST be `static class` with `static` methods
- Tools MUST use `[McpServerToolType]` and `[McpServerTool]` attributes
- All parameters MUST have `[Description]` attribute
- Return type MUST be `Task<string>` using `ToolResponseFormatter`

## Service Implementation

### Rules
- Define interface in `Services/Interfaces.cs`
- Create implementation in separate file `Services/{Name}Service.cs`
- Register in `Server/ServerServices.cs` with `AddSingleton`
- Return `Result<T>` for fallible operations
- Inject `PathResolver` for path resolution
- Inject `IGuardrailService` for limit checking

## Key File Interactions

| When Modifying | Also Modify |
|----------------|-------------|
| `Services/Interfaces.cs` (new interface) | `ServerServices.cs` (registration) |
| `Config/RlmSettings.cs` (new settings) | `Services/GuardrailService.cs` (if limit-related) |
| New Tool in `Tools/` | Ensure service is registered |
| New Service | Add tests in `tests/.../Services/` |

## Prohibited Actions

- **DO NOT** call any LLM APIs from server
- **DO NOT** implement recursive reasoning loops internally
- **DO NOT** use mutable state except GuardrailService._callCount
- **DO NOT** throw exceptions for expected failures (use Result<T>)
- **DO NOT** skip CancellationToken parameter on async methods
- **DO NOT** use manual for loops when LINQ expresses intent better
