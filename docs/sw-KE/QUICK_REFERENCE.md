# Kadi ya Marejeo ya Haraka - Razor Pages Template

## ?? Kuanza (dakika 5)

```powershell
# 1. Run setup script
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build & run
dotnet build
dotnet run
```

## ?? Faili za Usanidi

| Faili | Kusudi | Imewekwa kwenye commit? |
|------|---------|------------|
| `appsettings.template.json` | Template yenye placeholders | ? Ndiyo |
| `appsettings.json` | Usanidi wako halisi | ? Hapana (imepuuzwa na git) |
| User Secrets | Values nyeti | ? Hapana (local pekee) |

## ?? Feature Flags (Washa/Zima Haraka)

Hariri sehemu ya `FeatureFlags` katika `appsettings.json`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // ?? Washa kwa auth
  "EnableNonceServices": false,  // ??? Washa kwa CSP
  "EnableCosmosDb": false,       // ?? Washa kwa database
  "EnableBlobStorage": false     // ?? Washa kwa files
}
```

## ?? Amri za User Secrets

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

## ?? Secrets Zinazohitajika kwa Kila Kipengele

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

## ?? Scripts Muhimu

| Script | Kusudi | Matumizi |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | Usanidi wa awali | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Badilisha namespace | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Zalisha keys | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Kokotoa hashes za CSP | `.\HashInlineScriptPowerShell.ps1` |

## ??? Awamu za Development

### Awamu ya 1: Msingi (usanidi wa dakika 5)
- ? Session
- ? Localization
- ? Security headers
- ? Hakuna auth
- ? Hakuna database

**Usanidi**: Flags zote `false` isipokuwa `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Awamu ya 2: + Authentication (usanidi wa dakika 30)
- ? Vipengele vya Awamu ya 1
- ? Azure AD
- ? Authorization
- ? CSP + Nonce
- ? Hakuna database

**Usanidi**: Wezesha `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

**Inahitaji**:
- Azure AD app registration
- Encryption keys zilizozalishwa

### Awamu ya 3: + Azure Services (usanidi wa saa 1-2)
- ? Vipengele vya Awamu ya 2
- ? Cosmos DB
- ? Blob Storage
- ? Key Vault

**Usanidi**: Wezesha `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

**Inahitaji**:
- Azure resources zimeundwa
- Connection strings katika User Secrets

## ?? Utatuzi wa Haraka wa Matatizo

### Makosa ya Build
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

### Auth loop / makosa ya 401
1. Angalia Azure AD redirect URI inalingana
2. Thibitisha `EnableAzureAd: true` katika appsettings.json
3. Angalia client secret katika User Secrets
4. Futa browser cookies

### Ukiukaji wa CSP
1. Thibitisha `EnableNonceServices: true`
2. Angalia encryption keys zimewekwa
3. Kagua browser console kwa makosa ya CSP
4. Zima CSP kwa muda ili kujaribu: `EnableCSP: false`

## ?? Hati

- **Hati kamili**: `TEMPLATE_README.md`
- **Usanidi**: `appsettings.template.json`
- **Namespace**: Endesha `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ?? Orodha ya Ukaguzi wa Usalama

Kabla ya kusambaza kwenye production:

- [ ] Secrets zote ziko katika Azure Key Vault au User Secrets
- [ ] `appsettings.json` imepuuzwa na git
- [ ] `.gitignore` inajumuisha ignores mahususi za template
- [ ] Security headers zimewezeshwa
- [ ] CSP imesanidiwa kwa kutumia nonces
- [ ] HTTPS imelazimishwa
- [ ] Authentication imewezeshwa kwa kurasa zilizolindwa
- [ ] Secrets zimezungushwa kutoka default values

## ?? Vidokezo

- **Anza rahisi**: Anza na Awamu ya 1, ongeza vipengele hatua kwa hatua
- **Tumia WhatIf**: Jaribu scripts kwa `-WhatIf` kabla ya kutumia
- **Angalia kumbukumbu**: Wezesha `"Default": "Debug"` katika `Logging:LogLevel` kwa utatuzi wa matatizo
- **Thibitisha secrets**: Endesha `dotnet user-secrets list` kuona kilichosanidiwa
- **Clean builds**: Ukiona makosa ya ajabu, jaribu `dotnet clean && dotnet build`

## ?? Msaada

1. Soma `TEMPLATE_README.md`
2. Angalia maoni katika `appsettings.template.json`
3. Endesha `dotnet user-secrets list`
4. Wezesha debug logging
5. Angalia Azure Portal kwa hali ya resources

---

**Toleo la Template**: 1.0  
**ASP.NET Core**: 9.0  
**Ilisasishwa Mwisho**: 2024-12-20

