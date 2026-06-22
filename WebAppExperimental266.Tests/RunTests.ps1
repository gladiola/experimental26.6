<#
.SYNOPSIS
    Comprehensive test runner for WebAppExperimental266 test suite

.DESCRIPTION
    Runs all unit tests with various options including coverage, filtering, and reporting

.PARAMETER RunAll
    Run all tests

.PARAMETER Coverage
    Run tests with code coverage analysis

.PARAMETER Models
    Run only model tests

.PARAMETER Services
    Run only service tests

.PARAMETER Verbose
    Show detailed test output

.PARAMETER WhatIf
    Show what tests would be run without running them

.EXAMPLE
    .\RunTests.ps1 -RunAll
    Runs all tests

.EXAMPLE
    .\RunTests.ps1 -Coverage
    Runs all tests with coverage report

.EXAMPLE
    .\RunTests.ps1 -Models -Verbose
    Runs model tests with detailed output
#>

[CmdletBinding()]
param(
    [switch]$RunAll,
    [switch]$Coverage,
    [switch]$Models,
    [switch]$Services,
    [switch]$Verbose,
    [switch]$WhatIf
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Color functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Failure { param($Message) Write-Host $Message -ForegroundColor Red }

# Header
Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "  WebAppExperimental266 Test Suite Runner" -ForegroundColor White
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Determine test directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectDir = Join-Path $scriptDir ".." "WebAppExperimental266.Tests"
$testProjectFile = Join-Path $testProjectDir "WebAppExperimental266.Tests.csproj"

if (-not (Test-Path $testProjectFile)) {
    Write-Failure "? Test project not found at: $testProjectFile"
    exit 1
}

Write-Info "?? Test Project: $testProjectDir"
Write-Host ""

# Build test command
$testCommand = "dotnet test `"$testProjectFile`""

# Add filter if specific category requested
if ($Models) {
    Write-Info "?? Filter: Models only"
    $testCommand += " --filter `"FullyQualifiedName~Models`""
}
elseif ($Services) {
    Write-Info "?? Filter: Services only"
    $testCommand += " --filter `"FullyQualifiedName~Services`""
}

# Add coverage if requested
if ($Coverage) {
    Write-Info "?? Code coverage: Enabled"
    $testCommand += " /p:CollectCoverage=true /p:CoverletOutputFormat=opencover"
}

# Add verbosity
if ($Verbose) {
    Write-Info "?? Verbosity: Detailed"
    $testCommand += " --verbosity detailed"
}
else {
    $testCommand += " --verbosity normal"
}

# Show command
if ($WhatIf) {
    Write-Info "Would run command:"
    Write-Host $testCommand -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Run tests
Write-Info "?? Running tests..."
Write-Host ""

try {
    # Clean first
    Write-Info "?? Cleaning..."
    $cleanCommand = "dotnet clean `"$testProjectFile`" --verbosity quiet"
    Invoke-Expression $cleanCommand | Out-Null

    # Restore
    Write-Info "?? Restoring packages..."
    $restoreCommand = "dotnet restore `"$testProjectFile`" --verbosity quiet"
    Invoke-Expression $restoreCommand | Out-Null

    Write-Host ""
    Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""

    # Run tests
    $startTime = Get-Date
    $output = Invoke-Expression $testCommand 2>&1
    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    # Display output
    $output | ForEach-Object {
        $line = $_.ToString()
        if ($line -match "Passed!") {
            Write-Success $line
        }
        elseif ($line -match "Failed!") {
            Write-Failure $line
        }
        elseif ($line -match "Total tests:") {
            Write-Info $line
        }
        else {
            Write-Host $line
        }
    }

    Write-Host ""
    Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""

    # Summary
    if ($exitCode -eq 0) {
        Write-Success "? All tests passed!"
        Write-Info "??  Duration: $([math]::Round($duration, 2)) seconds"
        
        # Parse results
        $resultsLine = $output | Where-Object { $_ -match "Passed!" -or $_ -match "Failed!" } | Select-Object -First 1
        if ($resultsLine) {
            Write-Host ""
            Write-Success "?? Results:"
            Write-Host "   $resultsLine" -ForegroundColor White
        }

        # Coverage report location
        if ($Coverage) {
            $coverageFile = Join-Path $testProjectDir "coverage.opencover.xml"
            if (Test-Path $coverageFile) {
                Write-Host ""
                Write-Info "?? Coverage report:"
                Write-Host "   $coverageFile" -ForegroundColor White
                Write-Info "?? Tip: Use ReportGenerator to create HTML report"
                Write-Host "   dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray
                Write-Host "   reportgenerator -reports:`"$coverageFile`" -targetdir:`"coverage-report`"" -ForegroundColor Gray
            }
        }
    }
    else {
        Write-Failure "? Tests failed!"
        Write-Info "??  Duration: $([math]::Round($duration, 2)) seconds"
        
        Write-Host ""
        Write-Warning "?? To debug:"
        Write-Host "   dotnet test --filter `"FullyQualifiedName~FailedTestName`"" -ForegroundColor Yellow
        
        exit $exitCode
    }
}
catch {
    Write-Failure "? Error running tests:"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Additional commands
Write-Info "?? Useful commands:"
Write-Host ""
Write-Host "  List all tests:" -ForegroundColor White
Write-Host "    dotnet test --list-tests" -ForegroundColor Gray
Write-Host ""
Write-Host "  Run specific test:" -ForegroundColor White
Write-Host "    dotnet test --filter `"FullyQualifiedName~TestName`"" -ForegroundColor Gray
Write-Host ""
Write-Host "  Watch mode:" -ForegroundColor White
Write-Host "    dotnet watch test" -ForegroundColor Gray
Write-Host ""
Write-Host "  Debug test:" -ForegroundColor White
Write-Host "    dotnet test --logger `"console;verbosity=detailed`"" -ForegroundColor Gray
Write-Host ""

Write-Success "? Test run complete!"
Write-Host ""

exit 0
