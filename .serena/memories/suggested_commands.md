# Suggested Commands

## Build Commands
```powershell
# Build entire solution
dotnet build

# Build specific project
dotnet build src/RecursiveContext.Mcp.Server

# Clean and rebuild
dotnet clean && dotnet build
```

## Test Commands
```powershell
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests/RecursiveContext.Mcp.Server.Tests
```

## Run Commands
```powershell
# Run the MCP server (for testing)
dotnet run --project src/RecursiveContext.Mcp.Server

# Run with help
dotnet run --project src/RecursiveContext.Mcp.Server -- --help

# Run with version
dotnet run --project src/RecursiveContext.Mcp.Server -- --version
```

## Utility Commands (Windows)
```powershell
# List files
dir
Get-ChildItem

# Search for text in files
Select-String -Path "*.cs" -Pattern "pattern"

# Find files by name
Get-ChildItem -Recurse -Filter "*.cs"

# Git commands (same as Unix)
git status
git diff
git log --oneline -10
```

## File Operations with PowerShell
```powershell
# Create directory
New-Item -ItemType Directory -Path "path"

# Create/overwrite file
Set-Content -Path "file.cs" -Value "content"

# Read file
Get-Content -Path "file.cs"

# Delete file
Remove-Item -Path "file.cs"
```

## Working Directory
Project root: `C:\projects\github\mcp-recursive-context`
