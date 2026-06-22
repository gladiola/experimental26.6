# Kāri Tohutoro Tere - Tātauira Razor Pages

## 🚀 Tīmatanga Tere (5 meneti)

```powershell
# 1. Whakahaerehia te hōtuhi tatūnga
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Hanga & whakahaere
dotnet build
dotnet run
```

## 📁 Kōnae Whirihoranga

| Kōnae | Kaupapa | Ka commit? |
|------|---------|------------|
| `appsettings.template.json` | Tātauira whai tūtohu | ✅ Āe |
| `appsettings.json` | Ō whirihoranga tūturu | ❌ Kāo (git-ignored) |
| User Secrets | Uara tairongo | ❌ Kāo (ā-rohe anake) |

## 🎛️ Haki Āhuatanga (Whakahohe/Whakaweto Tere)

Whakatikahia te wāhanga `FeatureFlags` i `appsettings.json`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## 🔐 Ngā Tono User Secrets

```powershell
# Tīmata
dotnet user-secrets init

# Tautuhi muna
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# Rārangi muna katoa
dotnet user-secrets list

# Tango muna
dotnet user-secrets remove "AzureAd:ClientSecret"

# Mukua katoa
dotnet user-secrets clear
```

## 🧩 Ngā Muna e Hiahiatia ana mā ia Āhuatanga

### Whakamana Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Waihangatia tuatahi: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## 🛠️ Ngā Hōtuhi Whaihua

| Hōtuhi | Kaupapa | Whakamahi |
|--------|---------|----------|
| `SetupFromTemplate.ps1` | Tatūnga tuatahi | `./SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Panoni namespace | `./RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Waihanga kī/IV | `./IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Tatau hash CSP | `./HashInlineScriptPowerShell.ps1` |

## 🧭 Ngā Wāhanga Whakawhanake

### Wāhanga 1: Taketake (5 meneti)
- ✅ Session
- ✅ Localization
- ✅ Security headers
- ✅ Kāore he auth
- ✅ Kāore he database

**Whirihoranga**: `false` katoa, engari `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Wāhanga 2: + Whakamana (30 meneti)
- ✅ Āhuatanga Wāhanga 1
- ✅ Azure AD
- ✅ Authorization
- ✅ CSP + Nonce
- ✅ Kāore he database

**Whirihoranga**: Whakahohea `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

### Wāhanga 3: + Ratonga Azure (1-2 hāora)
- ✅ Āhuatanga Wāhanga 2
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**Whirihoranga**: Whakahohea `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

## 🩺 Rapurongoā Tere

### Hapa Build
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

### Auth loop / hapa 401
1. Tirohia te redirect URI Azure AD
2. Whakau `EnableAzureAd: true` i appsettings.json
3. Tirohia te client secret i User Secrets
4. Mukua pihikete pūtirotiro

### Takahitanga CSP
1. Whakau `EnableNonceServices: true`
2. Tirohia kua tautuhia ngā kī whakamunatanga
3. Arotakehia te console pūtirotiro
4. Whakamātauria me `EnableCSP: false`

## 📚 Tuhinga

- **Tuhinga katoa**: `TEMPLATE_README.md`
- **Whirihoranga**: `appsettings.template.json`
- **Namespace**: `./RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ✅ Rārangi Arowhai Haumaru

I mua i te tukuna ki te production:

- [ ] Kei Azure Key Vault/User Secrets ngā muna katoa
- [ ] Kua git-ignore a `appsettings.json`
- [ ] Kei `.gitignore` ngā ture tātauira
- [ ] Kua whakahohea ngā security headers
- [ ] Kua whirihora te CSP me ngā nonce
- [ ] Kua akiakina te HTTPS
- [ ] Kua whakahohea te whakamana mō ngā whārangi tiakina
- [ ] Kua hurihia ngā muna taunoa

## 💡 Tohu

- **Tīmata māmā**: tīmata ki Wāhanga 1, tāpiri āta
- **Whakamahia WhatIf** i mua i te tono hōtuhi
- **Tirohia ngā logs**: tautuhia `"Default": "Debug"`
- **Whakau ngā muna**: whakahaere `dotnet user-secrets list`
- **Mahi clean build** ina kitea ngā hapa rerekē

## 🆘 Āwhina

1. Pānuihia `TEMPLATE_README.md`
2. Tirohia ngā kōrero i `appsettings.template.json`
3. Whakahaere `dotnet user-secrets list`
4. Whakahohea debug logging
5. Tirohia te Azure Portal mō te āhua rawa

---

**Putanga Tātauira**: 1.0  
**ASP.NET Core**: 9.0  
**Whakahoutanga Whakamutunga**: 2024-12-20
