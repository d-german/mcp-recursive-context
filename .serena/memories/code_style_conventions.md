# Code Style and Conventions

## General Principles
- **SOLID Design** - All code follows SOLID principles
- **Functional Programming** - Favor immutability, pure functions, higher-order functions
- **Static Methods** - Methods not accessing instance state MUST be static

## C# Conventions
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled
- **Treat Warnings As Errors**: True in Directory.Build.props
- **Target**: .NET 9.0

## Result Pattern (Railway-Oriented)
- Use `CSharpFunctionalExtensions.Result<T>` for operations that can fail
- Chain operations with `.Bind()` and `.Map()`
- Avoid exceptions for expected failure cases
- Use `Task.FromResult()` for synchronous operations returning Result

## Collections
- Use `ImmutableArray<T>` and `ImmutableDictionary<K,V>` for return types
- Records are immutable (use `init` properties)

## Async/Await
- Always use `.ConfigureAwait(false)` when applicable
- All I/O operations are async
- Accept `CancellationToken` in all async methods

## MCP Tool Pattern
```csharp
[McpServerToolType]
public static class SomeToolName
{
    [McpServerTool(Name = "tool_name")]
    [Description("Tool description")]
    public static async Task<string> ExecuteAsync(
        IService service,
        [Description("Parameter description")] string parameter,
        CancellationToken ct)
    {
        return await service.DoWorkAsync(parameter, ct)
            .Match(
                success => ToolResponseFormatter.FormatSuccess(success),
                error => ToolResponseFormatter.FormatError(error));
    }
}
```

## Naming
- PascalCase for types, methods, properties
- camelCase for local variables and parameters  
- Suffix `Async` for async methods
- Prefix `I` for interfaces
- Suffix `Service` for service classes
- Suffix `Tool` for MCP tool classes

## XML Documentation
- All public types and members have XML doc comments
- `<summary>` required
- `<param>` for parameters
- `<returns>` for return values
