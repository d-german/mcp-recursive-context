# RecursiveContext.Mcp Server - Project Overview

## Purpose
A Model Context Protocol (MCP) server for recursive reasoning over massive offline context. Enables LLMs to systematically search, traverse, and reason over large codebases or document sets without loading that context into the model's prompt.

Based on the "Recursive Language Models" paper by Zhang, Kraska, and Khattab (Dec 2025).

## Key Concept
The server provides read-only file exploration tools. **The recursive reasoning behavior emerges from the client-side LLM**, not from server logic. The server just provides tools - the LLM decides how to chain them.

## Architecture
```
RecursiveContext.Mcp.sln
├── src/
│   └── RecursiveContext.Mcp.Server/
│       ├── Config/          # Configuration (RlmSettings, ConfigReader, PathResolver)
│       ├── Models/           # Immutable domain records
│       ├── Server/           # Host, services, logging setup
│       ├── Services/         # Business logic implementations
│       └── Tools/            # MCP tool implementations
│           ├── DotNet/       # .NET-specific tools
│           ├── FileSystem/   # File/directory listing tools
│           ├── Metadata/     # Context info tool
│           ├── Search/       # Pattern matching tools
│           └── Server/       # Server info tool
└── tests/
    └── RecursiveContext.Mcp.Server.Tests/
```

## Tech Stack
- **.NET 9.0** with C# latest language version
- **ModelContextProtocol 0.5.0-preview.1** - MCP server framework (stdio transport)
- **CSharpFunctionalExtensions 3.4.0** - Railway-oriented programming with Result<T>
- **Microsoft.Extensions.Hosting 10.0.1** - Host builder for DI and lifetime
- **xUnit** - Test framework

## MCP Tools Implemented
1. `list_files` - List files in a directory with pagination
2. `list_directories` - List subdirectories
3. `read_file` - Read entire file contents
4. `read_file_chunk` - Read specific line range
5. `get_context_info` - Get workspace metadata
6. `find_files_by_pattern` - Glob pattern file search
7. `enumerate_controllers` - Find ASP.NET controllers
8. `enumerate_endpoints` - Find HTTP endpoints
9. `get_server_info` - Server version/status
