# Quick Reference Card - Razor Pages Template

## ?? Getting Started (5 minutes)

```powershell
# 1. Run setup script
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build & run
dotnet build
dotnet run
```

## ?? Configuration Files

| File | Purpose | Committed? |
|------|---------|------------|
| `appsettings.template.json` | Template with placeholders | ? Yes |
| `appsettings.json` | Your actual config | ? No (git-ignored) |
| User Secrets | Sensitive values | ? No (local only) |

## ?? Feature Flags (Quick Enable/Disable)

Edit `appsettings.json` ? `FeatureFlags` section:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // ?? Turn on for auth
  "EnableNonceServices": false,  // ??? Turn on for CSP
  "EnableCosmosDb": false,       // ?? Turn on for database
  "EnableBlobStorage": false     // ?? Turn on for files
}
```

## ?? User Secrets Commands

```powershell
# Initialize
dotnet user-secrets init

# Set a secret
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "AzureAd:ClientSecret"

# Clear all secrets
dotnet user-secrets clear
```

## ?? Required Secrets by Feature

### Azure AD Authentication
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Generate first: .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## ?? Useful Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | Initial setup | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Change namespace | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Generate keys | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Calculate CSP hashes | `.\HashInlineScriptPowerShell.ps1` |

## ??? Development Phases

### Phase 1: Basic (5 min setup)
- ? Session
- ? Localization
- ? Security headers
- ? No auth
- ? No database

**Config**: All flags `false` except `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Phase 2: + Authentication (30 min setup)
- ? Phase 1 features
- ? Azure AD
- ? Authorization
- ? CSP + Nonce
- ? No database

**Config**: Enable `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

**Requires**:
- Azure AD app registration
- Generated encryption keys

### Phase 3: + Azure Services (1-2 hr setup)
- ? Phase 2 features
- ? Cosmos DB
- ? Blob Storage
- ? Key Vault

**Config**: Enable `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

**Requires**:
- Azure resources created
- Connection strings in User Secrets

## ?? Quick Troubleshooting

### Build Errors
```powershell
# Clean and rebuild
dotnet clean
dotnet build

# Check for missing packages
dotnet restore
```

### "Configuration not found"
```powershell
# Verify file exists
Test-Path appsettings.json

# If missing, copy from template
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# List secrets
dotnet user-secrets list

# Re-run setup
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth loop / 401 errors
1. Check Azure AD redirect URI matches
2. Verify `EnableAzureAd: true` in appsettings.json
3. Check client secret in User Secrets
4. Clear browser cookies

### CSP violations
1. Verify `EnableNonceServices: true`
2. Check encryption keys are set
3. Review browser console for CSP errors
4. Temporarily disable CSP to test: `EnableCSP: false`

## ?? Documentation

- **Full docs**: `TEMPLATE_README.md`
- **Configuration**: `appsettings.template.json`
- **Namespace**: Run `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ?? Security Checklist

Before deploying to production:

- [ ] All secrets in Azure Key Vault or User Secrets
- [ ] `appsettings.json` is git-ignored
- [ ] `.gitignore` includes template-specific ignores
- [ ] Security headers enabled
- [ ] CSP configured with nonces
- [ ] HTTPS enforced
- [ ] Authentication enabled for protected pages
- [ ] Secrets rotated from defaults

## ?? Tips

- **Start simple**: Begin with Phase 1, add features incrementally
- **Use WhatIf**: Test scripts with `-WhatIf` before applying
- **Check logs**: Enable `"Default": "Debug"` in `Logging:LogLevel` for troubleshooting
- **Verify secrets**: Run `dotnet user-secrets list` to see what's configured
- **Clean builds**: If weird errors, try `dotnet clean && dotnet build`

## ?? Help

1. Read `TEMPLATE_README.md`
2. Check `appsettings.template.json` comments
3. Run `dotnet user-secrets list`
4. Enable debug logging
5. Check Azure Portal for resource status

---

**Template Version**: 1.0  
**ASP.NET Core**: 9.0  
**Last Updated**: 2026-05-07
