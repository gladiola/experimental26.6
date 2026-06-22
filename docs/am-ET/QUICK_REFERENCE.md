# ፈጣን ማጣቀሻ (Quick Reference) — Razor Pages Template

## በ5 ደቂቃ ውስጥ መጀመር

```powershell
# 1. setup script አስኪድ
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build እና run
dotnet build
dotnet run
```

## የconfiguration ፋይሎች

| ፋይል | ዓላማ | commit ይደረግ? |
|---|---|---|
| `appsettings.template.json` | placeholders ያለ template | አዎ |
| `appsettings.json` | እውነተኛ ቅንብር | አይ (git-ignored) |
| User Secrets | ሚስጥር ዋጋዎች | አይ |

## Feature flags (ፈጣን ማብራት/ማጥፋት)

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## User Secrets ትዕዛዞች

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## አስፈላጊ secrets በfeature

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# መጀመሪያ: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## ጠቃሚ scripts

| Script | ዓላማ | ምሳሌ |
|---|---|---|
| `SetupFromTemplate.ps1` | የመጀመሪያ ማቀናበር | `./SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | namespace መቀየር | `./RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | key/IV ማመንጨት | `./IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | CSP hash ማመንጨት | `./HashInlineScriptPowerShell.ps1` |

## የልማት ደረጃዎች

### ደረጃ 1: Basic
- Session
- Localization
- Security headers
- ማረጋገጫ የለም
- Database የለም

### ደረጃ 2: + Authentication
- Azure AD
- Authorization
- CSP + Nonce

### ደረጃ 3: + Azure services
- Cosmos DB
- Blob Storage
- Key Vault

## ፈጣን መፍትሄ (Troubleshooting)

### Build errors
```powershell
dotnet clean
dotnet build
dotnet restore
```

### “Configuration not found”
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### “Secret not found”
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth loop / 401
1. Azure AD redirect URI ይመርምሩ
2. `EnableAzureAd: true` እንደሆነ ያረጋግጡ
3. client secret ይፈትሹ
4. browser cookies ያጥፉ

### CSP violations
1. `EnableNonceServices: true` እንደሆነ ያረጋግጡ
2. encryption keys እንደተቀናበሩ ያረጋግጡ
3. browser console ይመልከቱ
4. ለሙከራ ብቻ CSP ጊዜያዊ ያጥፉ

## ሰነዶች

- ሙሉ docs: `TEMPLATE_README.md`
- ቅንብር ማጣቀሻ: `appsettings.template.json`

## ከproduction በፊት የደህንነት checklist

- [ ] ሁሉም secrets በUser Secrets ወይም Azure Key Vault ውስጥ ናቸው
- [ ] `appsettings.json` git-ignored ነው
- [ ] security headers በርተዋል
- [ ] CSP + nonces ተዋቅረዋል
- [ ] HTTPS ተግዷል
- [ ] የተጠበቁ ገጾች ላይ auth ነቅቷል
- [ ] default secrets ተቀይረዋል

## ምክሮች

- በደረጃ 1 ጀምሩ፣ ቀስ በቀስ ባህሪያት ያክሉ
- scripts ካሉ `-WhatIf` በመጠቀም ይሞክሩ
- troubleshooting ጊዜ debug logs ያብሩ
- `dotnet user-secrets list` በመጠቀም ቅንብሮችን ያረጋግጡ

---

**Template Version:** 1.0  
**ASP.NET Core:** 9.0  
**Last Updated:** 2026-05-07
