# Task Completion Checklist

When completing a task, run these commands in order:

## 1. Build Check
```powershell
dotnet build
```
- Must complete with 0 errors and 0 warnings
- TreatWarningsAsErrors is enabled

## 2. Run Tests
```powershell
dotnet test
```
- All tests must pass
- No test failures allowed

## 3. Verify With Task Manager
After completing a task:
1. Use `mcp_task-and-rese_verify_task` with the taskId
2. Provide a score (0-100) and summary
3. Move to next pending task

## Pre-Commit Checks
- [ ] Build succeeds with 0 warnings
- [ ] All tests pass
- [ ] New code follows conventions in code_style_conventions.md
- [ ] Public members have XML documentation
- [ ] Result<T> used for fallible operations
- [ ] Async methods accept CancellationToken
- [ ] Static methods marked static
- [ ] Immutable collections used for returns
