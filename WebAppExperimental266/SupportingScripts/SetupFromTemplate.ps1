<#
.SYNOPSIS
    Setup script to initialize project from template configuration
    
.DESCRIPTION
    This script helps you configure the Razor Pages template by:
    - Replacing template placeholders with your values
    - Setting up User Secrets securely
    - Generating encryption keys
    - Validating configuration
    
.PARAMETER ProjectName
    Name of your project (used for namespace customization)
    
.PARAMETER TenantId
    Azure AD Tenant ID (GUID)
    
.PARAMETER ClientId
    Azure AD Application/Client ID (GUID)
    
.PARAMETER TenantDomain
    Azure AD Tenant Domain (e.g., contoso)
    
.PARAMETER GenerateKeys
    Generate encryption keys for nonce services
    
.PARAMETER SkipSecrets
    Skip User Secrets setup (configure manually later)
    
.EXAMPLE
    .\SetupFromTemplate.ps1 -ProjectName "MyApp" -GenerateKeys
    
.EXAMPLE
    .\SetupFromTemplate.ps1 -TenantId "xxx" -ClientId "yyy" -TenantDomain "contoso" -SkipSecrets
#>

param(
    [string]$ProjectName = "",
    [string]$TenantId = "",
    [string]$ClientId = "",
    [string]$TenantDomain = "",
    [switch]$GenerateKeys = $false,
    [switch]$SkipSecrets = $false
)

$ErrorActionPreference = "Stop"

# Colors
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"

# Banner
Write-Host ""
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host "?     ASP.NET Core 9 Razor Pages Template Setup            ?" -ForegroundColor $ColorInfo
Write-Host "?     Version 1.0                                           ?" -ForegroundColor $ColorInfo
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host ""

# Helper function to generate random base64 key
function New-RandomBase64Key {
    param([int]$ByteLength)
    $bytes = New-Object byte[] $ByteLength
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes)
}

# Helper function to mask sensitive input
function Read-HostSecure {
    param([string]$Prompt)
    $secure = Read-Host $Prompt -AsSecureString
    return [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

# Validate paths
$scriptDir = $PSScriptRoot
$projectRoot = Split-Path $scriptDir -Parent
$templatePath = Join-Path $projectRoot "appsettings.template.json"
$appSettingsPath = Join-Path $projectRoot "appsettings.json"

if (!(Test-Path $templatePath)) {
    Write-Host "? Template file not found: $templatePath" -ForegroundColor $ColorError
    exit 1
}

# Check if appsettings.json exists
if (Test-Path $appSettingsPath) {
    Write-Host "? Warning: appsettings.json already exists!" -ForegroundColor $ColorWarning
    $overwrite = Read-Host "  Do you want to overwrite it? (y/N)"
    if ($overwrite -ne 'y') {
        Write-Host "? Setup cancelled." -ForegroundColor $ColorError
        exit 0
    }
}

Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host " Step 1: Collect Configuration Values" -ForegroundColor $ColorInfo
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host ""
Write-Host "Press Enter to skip optional values" -ForegroundColor $ColorWarning
Write-Host ""

# Collect basic configuration
if ([string]::IsNullOrEmpty($ProjectName)) {
    $ProjectName = Read-Host "Project Name (for namespace, e.g., 'MyCompany.MyApp')"
}

if ([string]::IsNullOrEmpty($TenantId)) {
    $TenantId = Read-Host "Azure AD Tenant ID (optional)"
}

if ([string]::IsNullOrEmpty($ClientId)) {
    $ClientId = Read-Host "Azure AD Client/Application ID (optional)"
}

if ([string]::IsNullOrEmpty($TenantDomain)) {
    $TenantDomain = Read-Host "Azure AD Tenant Domain (e.g., 'contoso', optional)"
}

# Optional Azure resources
$KeyVaultName = Read-Host "Azure Key Vault Name (optional)"
$CosmosAccountName = Read-Host "Cosmos DB Account Name (optional)"
$DatabaseName = Read-Host "Cosmos DB Database Name (optional)"
$ContainerName = Read-Host "Cosmos DB Container Name (optional)"

# Generate encryption keys if requested
$NonceKey = ""
$NonceIV = ""

if ($GenerateKeys) {
    Write-Host ""
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host " Step 2: Generate Encryption Keys" -ForegroundColor $ColorInfo
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host ""
    Write-Host "Generating encryption keys for nonce services..." -ForegroundColor $ColorInfo
    
    $NonceKey = New-RandomBase64Key -ByteLength 32
    $NonceIV = New-RandomBase64Key -ByteLength 16
    
    Write-Host "? Generated 32-byte Key" -ForegroundColor $ColorSuccess
    Write-Host "? Generated 16-byte IV" -ForegroundColor $ColorSuccess
}

# Load and process template
Write-Host ""
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host " Step 3: Process Template" -ForegroundColor $ColorInfo
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
Write-Host ""

$template = Get-Content $templatePath -Raw

# Replace placeholders
$config = $template

if (![string]::IsNullOrEmpty($TenantId)) {
    $config = $config -replace '\{\{YOUR_TENANT_ID\}\}', $TenantId
    Write-Host "? Set Tenant ID" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($ClientId)) {
    $config = $config -replace '\{\{YOUR_CLIENT_ID\}\}', $ClientId
    Write-Host "? Set Client ID" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($TenantDomain)) {
    $config = $config -replace '\{\{YOUR_TENANT_DOMAIN\}\}', $TenantDomain
    Write-Host "? Set Tenant Domain" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($KeyVaultName)) {
    $config = $config -replace '\{\{YOUR_KEYVAULT_NAME\}\}', $KeyVaultName
    Write-Host "? Set Key Vault Name" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($CosmosAccountName)) {
    $config = $config -replace '\{\{YOUR_COSMOSDB_ACCOUNT\}\}', $CosmosAccountName
    Write-Host "? Set Cosmos DB Account" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($DatabaseName)) {
    $config = $config -replace '\{\{YOUR_DATABASE_NAME\}\}', $DatabaseName
    Write-Host "? Set Database Name" -ForegroundColor $ColorSuccess
}

if (![string]::IsNullOrEmpty($ContainerName)) {
    $config = $config -replace '\{\{YOUR_CONTAINER_NAME\}\}', $ContainerName
    Write-Host "? Set Container Name" -ForegroundColor $ColorSuccess
}

if ($GenerateKeys) {
    $config = $config -replace '\{\{GENERATE_32_BYTE_BASE64_KEY\}\}', $NonceKey
    $config = $config -replace '\{\{GENERATE_16_BYTE_BASE64_IV\}\}', $NonceIV
    Write-Host "? Set Encryption Keys" -ForegroundColor $ColorSuccess
}

# Save appsettings.json
$config | Out-File -FilePath $appSettingsPath -Encoding UTF8
Write-Host ""
Write-Host "? Created appsettings.json" -ForegroundColor $ColorSuccess

# Setup User Secrets
if (!$SkipSecrets) {
    Write-Host ""
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host " Step 4: Configure User Secrets" -ForegroundColor $ColorInfo
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host ""
    Write-Host "Sensitive values should be stored in User Secrets, not appsettings.json" -ForegroundColor $ColorWarning
    Write-Host ""
    
    $setupSecrets = Read-Host "Configure User Secrets now? (Y/n)"
    
    if ($setupSecrets -ne 'n') {
        Push-Location $projectRoot
        
        Write-Host "Initializing User Secrets..." -ForegroundColor $ColorInfo
        dotnet user-secrets init
        
        Write-Host ""
        Write-Host "Enter sensitive values (will be stored securely):" -ForegroundColor $ColorInfo
        Write-Host ""
        
        # Azure AD Client Secret
        if (![string]::IsNullOrEmpty($ClientId)) {
            $ClientSecret = Read-HostSecure "  Azure AD Client Secret"
            if (![string]::IsNullOrEmpty($ClientSecret)) {
                dotnet user-secrets set "AzureAd:ClientSecret" $ClientSecret | Out-Null
                dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" $ClientSecret | Out-Null
                Write-Host "  ? Stored Azure AD Client Secret" -ForegroundColor $ColorSuccess
            }
        }
        
        # Key Vault Secret
        if (![string]::IsNullOrEmpty($KeyVaultName)) {
            Write-Host ""
            $KeyVaultSecret = Read-HostSecure "  Azure Key Vault Secret (Client Secret or Certificate)"
            if (![string]::IsNullOrEmpty($KeyVaultSecret)) {
                dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" $KeyVaultSecret | Out-Null
                Write-Host "  ? Stored Key Vault Secret" -ForegroundColor $ColorSuccess
            }
        }
        
        # Cosmos DB
        if (![string]::IsNullOrEmpty($CosmosAccountName)) {
            Write-Host ""
            Write-Host "  Cosmos DB Connection String:" -ForegroundColor $ColorWarning
            Write-Host "  (Paste entire connection string from Azure Portal)" -ForegroundColor $ColorWarning
            $CosmosConnection = Read-Host "  "
            
            if (![string]::IsNullOrEmpty($CosmosConnection)) {
                dotnet user-secrets set "CosmosDb:CosmosConnectionString" $CosmosConnection | Out-Null
                
                # Extract AccountKey if present
                if ($CosmosConnection -match "AccountKey=([^;]+)") {
                    $AccountKey = $matches[1]
                    dotnet user-secrets set "CosmosDb:AccountKey" $AccountKey | Out-Null
                }
                
                Write-Host "  ? Stored Cosmos DB Connection" -ForegroundColor $ColorSuccess
            }
        }
        
        # Blob Storage
        Write-Host ""
        $BlobConnection = Read-Host "  Blob Storage Connection String (optional, press Enter to skip)"
        if (![string]::IsNullOrEmpty($BlobConnection)) {
            dotnet user-secrets set "BlobSettings:BlobConnectionString" $BlobConnection | Out-Null
            Write-Host "  ? Stored Blob Storage Connection" -ForegroundColor $ColorSuccess
        }
        
        # Nonce Keys (if generated)
        if ($GenerateKeys) {
            Write-Host ""
            dotnet user-secrets set "NonceEncryption:Key" $NonceKey | Out-Null
            dotnet user-secrets set "NonceEncryption:IV" $NonceIV | Out-Null
            Write-Host "  ? Stored Encryption Keys" -ForegroundColor $ColorSuccess
        }
        
        Pop-Location
        
        Write-Host ""
        Write-Host "? User Secrets configured" -ForegroundColor $ColorSuccess
    }
}

# Namespace customization
if (![string]::IsNullOrEmpty($ProjectName) -and $ProjectName -ne "WebAppExperimental266") {
    Write-Host ""
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host " Step 5: Customize Namespace" -ForegroundColor $ColorInfo
    Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorInfo
    Write-Host ""
    
    $renameNamespace = Read-Host "Rename namespace from 'WebAppExperimental266' to '$ProjectName'? (Y/n)"
    
    if ($renameNamespace -ne 'n') {
        $renameScript = Join-Path $scriptDir "RenameNamespace.ps1"
        
        if (Test-Path $renameScript) {
            & $renameScript -NewNamespace $ProjectName
        } else {
            Write-Host "? RenameNamespace.ps1 not found, skipping" -ForegroundColor $ColorWarning
        }
    }
}

# Summary
Write-Host ""
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorSuccess
Write-Host " ? Setup Complete!" -ForegroundColor $ColorSuccess
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor $ColorSuccess
Write-Host ""
Write-Host "Next steps:" -ForegroundColor $ColorInfo
Write-Host ""
Write-Host "  1. Review appsettings.json and enable/disable features via FeatureFlags" -ForegroundColor "White"
Write-Host "  2. Verify User Secrets: dotnet user-secrets list" -ForegroundColor "White"
Write-Host "  3. Build the project: dotnet build" -ForegroundColor "White"
Write-Host "  4. Run the application: dotnet run" -ForegroundColor "White"
Write-Host ""
Write-Host "Documentation:" -ForegroundColor $ColorInfo
Write-Host "  - Read TEMPLATE_README.md for detailed configuration guide" -ForegroundColor "White"
Write-Host "  - Check appsettings.template.json for all available settings" -ForegroundColor "White"
Write-Host ""
Write-Host "? Security Reminder:" -ForegroundColor $ColorWarning
Write-Host "  - NEVER commit appsettings.json to source control" -ForegroundColor "White"
Write-Host "  - NEVER share User Secrets" -ForegroundColor "White"
Write-Host "  - Rotate secrets regularly" -ForegroundColor "White"
Write-Host ""
