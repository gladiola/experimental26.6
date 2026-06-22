# Snelstartkaart - Razor Pages-sjabloon

## 🚀 Aan de slag (5 minuten)

```powershell
# 1. Setupscript uitvoeren
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Builden en starten
dotnet build
dotnet run
```

## 📁 Configuratiebestanden

| Bestand | Doel | Gecommit? |
|------|---------|------------|
| `appsettings.template.json` | Sjabloon met placeholders | ✅ Ja |
| `appsettings.json` | Jouw daadwerkelijke configuratie | ❌ Nee (git-ignored) |
| User Secrets | Gevoelige waarden | ❌ Nee (alleen lokaal) |

## 🎛️ Featureflags (snel aan/uit)

Bewerk in `appsettings.json` de sectie `FeatureFlags`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 Aan voor authenticatie
  "EnableNonceServices": false,  // 🛡️ Aan voor CSP
  "EnableCosmosDb": false,       // 🗄️ Aan voor database
  "EnableBlobStorage": false     // 📦 Aan voor bestanden
}
```

## 🔑 User Secrets-commando's

```powershell
# Initialiseren
dotnet user-secrets init

# Geheim instellen
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# Alle geheimen tonen
dotnet user-secrets list

# Geheim verwijderen
dotnet user-secrets remove "AzureAd:ClientSecret"

# Alles wissen
dotnet user-secrets clear
```

## 🔒 Vereiste geheimen per feature

### Azure AD-authenticatie
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Eerst genereren: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## 🧰 Handige scripts

| Script | Doel | Gebruik |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | Eerste setup | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Namespace wijzigen | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Sleutels genereren | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | CSP-hashes berekenen | `.\HashInlineScriptPowerShell.ps1` |

## 🛣️ Ontwikkelfases

### Fase 1: Basis (5 min setup)
- ✅ Sessie
- ✅ Lokalisatie
- ✅ Security headers
- ❌ Geen auth
- ❌ Geen database

**Config**: Alle flags `false` behalve `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Fase 2: + Authenticatie (30 min setup)
- ✅ Features uit fase 1
- ✅ Azure AD
- ✅ Autorisatie
- ✅ CSP + Nonce
- ❌ Geen database

**Config**: Zet `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP` aan

**Vereist**:
- Azure AD app-registratie
- Gegenereerde encryptiesleutels

### Fase 3: + Azure-services (1-2 uur setup)
- ✅ Features uit fase 2
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**Config**: Zet `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault` aan

**Vereist**:
- Azure-resources aangemaakt
- Connection strings in User Secrets

## 🩺 Snelle troubleshooting

### Buildfouten
```powershell
# Opschonen en opnieuw builden
dotnet clean
dotnet build

# Ontbrekende packages controleren
dotnet restore
```

### "Configuration not found"
```powershell
# Controleren of bestand bestaat
Test-Path appsettings.json

# Als ontbreekt, kopiëren vanuit sjabloon
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# Geheimen tonen
dotnet user-secrets list

# Setup opnieuw uitvoeren
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth-loop / 401-fouten
1. Controleer of Azure AD redirect URI overeenkomt
2. Verifieer `EnableAzureAd: true` in appsettings.json
3. Controleer client secret in User Secrets
4. Wis browsercookies

### CSP-overtredingen
1. Verifieer `EnableNonceServices: true`
2. Controleer of encryptiesleutels zijn ingesteld
3. Bekijk browserconsole voor CSP-fouten
4. CSP tijdelijk uitzetten om te testen: `EnableCSP: false`

## 📚 Documentatie

- **Volledige docs**: `TEMPLATE_README.md`
- **Configuratie**: `appsettings.template.json`
- **Namespace**: Run `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ✅ Security-checklist

Voor productie-uitrol:

- [ ] Alle geheimen in Azure Key Vault of User Secrets
- [ ] `appsettings.json` staat in git-ignore
- [ ] `.gitignore` bevat template-specifieke ignores
- [ ] Security headers staan aan
- [ ] CSP met nonces is geconfigureerd
- [ ] HTTPS is afgedwongen
- [ ] Authenticatie staat aan voor beschermde pagina's
- [ ] Geheimen zijn geroteerd vanaf standaardwaarden

## 💡 Tips

- **Begin simpel**: start met fase 1, voeg features stapsgewijs toe
- **Gebruik WhatIf**: test scripts met `-WhatIf` voordat je toepast
- **Check logs**: zet `"Default": "Debug"` in `Logging:LogLevel` voor troubleshooting
- **Verifieer geheimen**: run `dotnet user-secrets list` om configuratie te zien
- **Schone builds**: bij rare fouten: `dotnet clean && dotnet build`

## 🆘 Hulp

1. Lees `TEMPLATE_README.md`
2. Controleer opmerkingen in `appsettings.template.json`
3. Run `dotnet user-secrets list`
4. Zet debug-logging aan
5. Controleer Azure Portal op resource-status

---

**Sjabloonversie**: 1.0  
**ASP.NET Core**: 9.0  
**Laatst bijgewerkt**: 2024-12-20
