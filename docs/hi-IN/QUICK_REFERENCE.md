# Quick Reference Card - Razor Pages Template

## तेज़ शुरुआत (5 मिनट)

```powershell
# 1) Setup script चलाएँ
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2) Build + Run
dotnet build
dotnet run
```

## कॉन्फ़िगरेशन फ़ाइलें

| फ़ाइल | उद्देश्य | Commit? |
|---|---|---|
| `appsettings.template.json` | placeholder template | ✅ Yes |
| `appsettings.json` | वास्तविक config | ❌ No |
| User Secrets | संवेदनशील मान | ❌ No |

## Feature Flags (त्वरित)

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## User Secrets कमांड

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## फ़ीचर-वार आवश्यक secrets

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# पहले key/iv जनरेट करें: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## उपयोगी scripts

| Script | उपयोग |
|---|---|
| `SetupFromTemplate.ps1` | प्रारंभिक setup |
| `RenameNamespace.ps1` | namespace बदलना |
| `IVandKeySampleGenerator.ps1` | key/iv generation |
| `HashInlineScriptPowerShell.ps1` | CSP hash generation |

## विकास चरण

### Phase 1: Basic
- Session, Localization, Security headers
- Auth/DB off

### Phase 2: + Auth
- Azure AD + Authorization + CSP/Nonce

### Phase 3: + Azure Services
- Cosmos DB + Blob + Key Vault

## त्वरित troubleshooting

### Build errors
```powershell
dotnet clean
dotnet build
dotnet restore
```

### "Configuration not found"
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth loop / 401
1. redirect URI match करें
2. `EnableAzureAd: true` जांचें
3. client secret जांचें
4. browser cookies clear करें

### CSP issues
1. `EnableNonceServices: true`
2. key/iv set
3. browser console errors देखें
4. परीक्षण हेतु अस्थायी `EnableCSP: false`

## सुरक्षा checklist

- [ ] secrets Key Vault या User Secrets में
- [ ] `appsettings.json` git-ignored
- [ ] security headers सक्षम
- [ ] CSP with nonces सक्षम
- [ ] HTTPS enforced
- [ ] protected pages पर auth enabled
- [ ] default secrets rotate

---

**Template Version**: 1.0  
**ASP.NET Core**: 9.0  
**Last Updated**: 2024-12-20
