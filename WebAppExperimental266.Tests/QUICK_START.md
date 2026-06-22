# Quick Start - Running Tests

## ?? Run All Tests

```powershell
cd WebAppExperimental266.Tests
dotnet test
```

## ?? Run with Coverage

```powershell
.\RunTests.ps1 -Coverage
```

## ?? Run Specific Tests

```powershell
# Models only
.\RunTests.ps1 -Models

# Services only
.\RunTests.ps1 -Services

# Verbose output
.\RunTests.ps1 -Verbose

# See what would run
.\RunTests.ps1 -WhatIf
```

## ?? Run Single Test

```powershell
dotnet test --filter "FullyQualifiedName~DataProcessingStatusTests"
```

## ?? Generate Coverage Report

```powershell
# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Install report generator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"coverage.opencover.xml" -targetdir:"coverage-report"

# Open report
start coverage-report\index.html
```

## ?? Watch Mode

```powershell
dotnet watch test
```

## ?? Debug Test

```powershell
dotnet test --logger "console;verbosity=detailed"
```

## ?? List All Tests

```powershell
dotnet test --list-tests
```

## ? Expected Results

```
Passed!  - Failed:     0, Passed:   130+, Skipped:     0, Total:   130+
Duration: < 5 seconds
```

## ?? Test Categories

| Category | Command | Tests |
|----------|---------|-------|
| All | `dotnet test` | 130+ |
| Models | `.\RunTests.ps1 -Models` | 59 |
| Services | `.\RunTests.ps1 -Services` | 71+ |

## ?? Tips

- Use `.\RunTests.ps1 -Help` for all options
- Run tests before committing code
- Keep coverage above 95%
- Add tests for new features
- Use TestHelpers for common scenarios

## ?? Troubleshooting

### Tests Not Found
```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

### Coverage Not Generated
```powershell
# Add coverlet.collector package
dotnet add package coverlet.collector
dotnet test /p:CollectCoverage=true
```

### Slow Tests
```powershell
# Run in parallel
dotnet test --parallel
```

---

**Quick Command**: `dotnet test`  
**With Coverage**: `.\RunTests.ps1 -Coverage`  
**Expected**: All tests passing ?
