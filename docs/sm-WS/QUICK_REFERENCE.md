# Kata Faavavevave — Razor Pages Template

## Amata vave (5 minute)

```powershell
# 1) Setup amata
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2) Build ma run
dotnet build
dotnet run
```

---

## Faila faatulagaga autu

| Faila | Faamoemoe | Commit? |
|---|---|---|
| `appsettings.template.json` | Template placeholders | Ioe |
| `appsettings.json` | Faatulagaga moni | Leai (gitignored) |
| User Secrets | Mea lilo i dev | Leai |

---

## Feature flags vave

I `FeatureFlags` e mafai ona e liliu ON/OFF:

- `EnableAzureAd`
- `EnableAuthorization`
- `EnableNonceServices`
- `EnableCSP`
- `EnableCosmosDb`
- `EnableBlobStorage`
- `EnableKeyVault`
- `EnableMtls`

---

## User secrets commands

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

---

## Mea lilo e manaʻomia e vaega

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

---

## Troubleshooting vave

### Build errors
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Missing config
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### Auth loop / 401
1. siaki redirect URI,
2. siaki feature flags,
3. siaki client secret,
4. clear browser cookies.

### CSP violations
1. ON `EnableNonceServices`,
2. faamaonia keys/nonce settings,
3. siaki browser console.

---

## Security checklist

- [ ] Secrets i User Secrets/Key Vault
- [ ] `appsettings.json` e le commit
- [ ] Security headers ON
- [ ] CSP nonce configured
- [ ] HTTPS faamalosia
- [ ] Authentication/authorization ON
- [ ] Secrets ua suia mai defaults
