# MCP Configuration Examples

This directory contains example MCP configuration files for various clients and platforms.

## GitHub Copilot (VS Code)

Copy the appropriate configuration to `.vscode/mcp.json` in your workspace.

### Using VS Code Variables (Recommended)

**[mcp.json](mcp.json)** - Uses `${workspaceFolder}` variable for portability:

```json
{
  "servers": {
    "recursive-context": {
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}/src/RecursiveContext.Mcp.Server"],
      "env": {
        "RLM_WORKSPACE_ROOT": "${workspaceFolder}"
      }
    }
  }
}
```

> **Note:** This works when the MCP server source is in your workspace. For a separate installation, use platform-specific paths below.

### Platform-Specific Examples

| File | Platform | Description |
|------|----------|-------------|
| [mcp.windows.json](mcp.windows.json) | Windows | Uses `C:/path/to/...` paths |
| [mcp.macos.json](mcp.macos.json) | macOS | Uses `/Users/username/...` paths |
| [mcp.linux.json](mcp.linux.json) | Linux | Uses `/home/username/...` paths |

### Using Published Executable

**[mcp.published-exe.json](mcp.published-exe.json)** - Uses a pre-built executable instead of `dotnet run`:

```json
{
  "servers": {
    "recursive-context": {
      "command": "C:/path/to/.../RecursiveContext.Mcp.Server.exe",
      "args": [],
      "env": {
        "RLM_WORKSPACE_ROOT": "${workspaceFolder}"
      }
    }
  }
}
```

To build the executable:
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Claude Desktop

**[claude_desktop_config.json](claude_desktop_config.json)**

Copy to your Claude Desktop configuration location:
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

> **Note:** Claude uses `mcpServers` (not `servers`) as the key name.

## Configuration Options

All examples support these environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `RLM_WORKSPACE_ROOT` | **Required.** Root directory for file access (sandboxed) | Current directory |
| `RLM_MAX_BYTES_PER_READ` | Maximum bytes per file read | `1048576` (1 MB) |
| `RLM_MAX_TOOL_CALLS` | Maximum tool calls per session | `1000` |
| `RLM_TIMEOUT_SECONDS` | Timeout for long operations | `30` |
| `RLM_MAX_DEPTH` | Maximum directory depth for recursion | `20` |

## Quick Start

1. Copy the appropriate example to your client's configuration location
2. Update paths to match your installation
3. Set `RLM_WORKSPACE_ROOT` to the codebase you want to explore
4. Restart your MCP client

## Troubleshooting

### "Server not found" or connection errors

- Verify the `command` path is correct and the executable exists
- For `dotnet run`, ensure .NET 9 SDK is installed: `dotnet --version`
- Check that the project path in `args` is absolute and correct

### "Path outside workspace" errors

- The server sandboxes all file access to `RLM_WORKSPACE_ROOT`
- Ensure this environment variable points to your target codebase
- Paths like `../../` that escape the workspace will be rejected

### No tools appearing

- Check server logs for startup errors
- Verify the MCP client has reloaded after configuration changes
- Try `get_server_info` tool to confirm the server is responding

## Testing Your Configuration

After configuring, try these tool calls:

1. `get_server_info` - Confirms server is running, shows limits
2. `get_context_info` - Scans workspace, shows file counts
3. `list_files` with path `.` - Lists root directory contents
4. `find_files_by_pattern` with pattern `*.cs` - Finds C# files
