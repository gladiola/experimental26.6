<#
.SYNOPSIS
    Rename the WebAppExperimental266 namespace throughout the project
    
.DESCRIPTION
    This script performs a find-and-replace operation across all C# files
    to change the namespace from WebAppExperimental266 to your custom namespace.
    
.PARAMETER NewNamespace
    The new namespace to use (e.g., "MyCompany.MyApp")
    
.PARAMETER WhatIf
    Show what would be changed without making changes
    
.EXAMPLE
    .\RenameNamespace.ps1 -NewNamespace "Contoso.WebApp"
    
.EXAMPLE
    .\RenameNamespace.ps1 -NewNamespace "MyCompany.MyApp" -WhatIf
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$NewNamespace,
    
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?     Namespace Renaming Tool                               ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

Write-Host "Current namespace: " -NoNewline
Write-Host "WebAppExperimental266" -ForegroundColor Yellow

Write-Host "New namespace:     " -NoNewline
Write-Host $NewNamespace -ForegroundColor Green
Write-Host ""

if ($WhatIf) {
    Write-Host "? Running in WhatIf mode - no changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot

# Find all C# files (excluding obj and bin folders)
Write-Host "Scanning for C# files..." -ForegroundColor Cyan
$csFiles = Get-ChildItem -Recurse -Filter *.cs | Where-Object {
    $_.FullName -notmatch '\\obj\\' -and
    $_.FullName -notmatch '\\bin\\' -and
    $_.FullName -notmatch '\\.vs\\'
}

Write-Host "Found $($csFiles.Count) C# files" -ForegroundColor Green
Write-Host ""

$filesModified = 0
$patternsFound = @{
    'namespace' = 0
    'using' = 0
}

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    # Check for namespace declarations
    if ($content -match "namespace\s+WebAppExperimental266") {
        $patternsFound['namespace']++
        $modified = $true
    }
    
    # Check for using directives
    if ($content -match "using\s+WebAppExperimental266") {
        $patternsFound['using']++
        $modified = $true
    }
    
    if ($modified) {
        $relativePath = $file.FullName.Substring($projectRoot.Length + 1)
        
        if ($WhatIf) {
            Write-Host "  [WOULD UPDATE] $relativePath" -ForegroundColor Yellow
        } else {
            Write-Host "  [UPDATING] $relativePath" -ForegroundColor Green
            
            # Perform replacements
            $newContent = $content -replace "namespace\s+WebAppExperimental266", "namespace $NewNamespace"
            $newContent = $newContent -replace "using\s+WebAppExperimental266", "using $NewNamespace"
            
            # Save with original encoding
            $newContent | Set-Content $file.FullName -NoNewline
        }
        
        $filesModified++
    }
}

Pop-Location

Write-Host ""
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

if ($WhatIf) {
    Write-Host "Files that would be modified: $filesModified" -ForegroundColor Yellow
} else {
    Write-Host "Files modified: $filesModified" -ForegroundColor Green
}

Write-Host "Namespace declarations found: $($patternsFound['namespace'])" -ForegroundColor White
Write-Host "Using directives found: $($patternsFound['using'])" -ForegroundColor White
Write-Host ""

if (!$WhatIf -and $filesModified -gt 0) {
    Write-Host "? Namespace successfully renamed to: $NewNamespace" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Build the project: dotnet build" -ForegroundColor White
    Write-Host "  2. Verify no compilation errors" -ForegroundColor White
    Write-Host "  3. Update project name in .csproj if desired" -ForegroundColor White
    Write-Host "  4. Commit changes to source control" -ForegroundColor White
} elseif ($WhatIf) {
    Write-Host "To apply these changes, run without -WhatIf:" -ForegroundColor Yellow
    Write-Host "  .\RenameNamespace.ps1 -NewNamespace '$NewNamespace'" -ForegroundColor White
} else {
    Write-Host "? No files were modified. The namespace may already be changed." -ForegroundColor Yellow
}

Write-Host ""
