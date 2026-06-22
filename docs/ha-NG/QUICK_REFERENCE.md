# Katin Saurin Tunani (Quick Reference) — Razor Pages Template

## Fara aiki cikin mintuna 5

```powershell
# 1. Gudanar da setup script
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build da run
dotnet build
dotnet run
```

## Fayilolin sanyi

| Fayil | Manufa | A commit? |
|---|---|---|
| `appsettings.template.json` | Template mai placeholders | Ee |
| `appsettings.json` | Ainihin saitinku | A'a (git-ignored) |
| User Secrets | Bayanai masu sirri | A'a |

## Feature flags (saurin kunna/kashewa)

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## Umarnin User Secrets

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## Muhimman secrets bisa fasali

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# fara da: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## Scripts masu amfani

| Script | Manufa | Misalin amfani |
|---|---|---|
| `SetupFromTemplate.ps1` | Saitin farko | `./SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Sauya namespace | `./RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Samar da maɓalli/IV | `./IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | CSP hashes | `./HashInlineScriptPowerShell.ps1` |

## Matakan ci gaba

### Mataki 1: Basic
- Session
- Localization
- Security headers
- Babu auth
- Babu database

### Mataki 2: + Authentication
- Azure AD
- Authorization
- CSP + Nonce

### Mataki 3: + Azure services
- Cosmos DB
- Blob Storage
- Key Vault

## Saurin warware matsala

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
1. Duba redirect URI na Azure AD
2. Tabbatar `EnableAzureAd: true`
3. Duba client secret
4. Goge browser cookies

### CSP violations
1. Tabbatar `EnableNonceServices: true`
2. Tabbatar encryption keys sun saita
3. Duba browser console
4. Don gwaji kawai, kashe CSP na ɗan lokaci

## Takardun bayani

- Cikakken docs: `TEMPLATE_README.md`
- Sanyi: `appsettings.template.json`

## Security checklist kafin production

- [ ] Duk secrets suna User Secrets ko Azure Key Vault
- [ ] `appsettings.json` yana git-ignored
- [ ] Security headers suna kunne
- [ ] CSP da nonces sun saita
- [ ] HTTPS an tilasta
- [ ] Auth an kunna ga shafukan kariya
- [ ] An juya default secrets

## Shawarwari

- Fara da sauƙi (Mataki 1), sannan ka ƙara a hankali
- Gwada scripts da `-WhatIf` idan akwai
- Kunna debug logs idan ana troubleshooting
- Duba `dotnet user-secrets list` don tabbatar da saiti

---

**Template Version:** 1.0
**ASP.NET Core:** 9.0
**Last Updated:** 2026-05-07
