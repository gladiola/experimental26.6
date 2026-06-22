# Vinnige Verwysingskaart — Razor Pages-Sjabloon

## 🚀 Begin (5 minute)

```powershell
# 1. Voer opstelskrif uit
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Bou & voer uit
dotnet build
dotnet run
```

## 📁 Konfigurasielêers

| Lêer | Doel | Gepleeg? |
|------|------|----------|
| `appsettings.template.json` | Sjabloon met plekhouers | ✅ Ja |
| `appsettings.json` | Jou werklike konfigurasie | ❌ Nee (git-geïgnoreer) |
| Gebruikersgeheime | Sensitiewe waardes | ❌ Nee (slegs plaaslik) |

## 🎛️ Funksievlaggies (Vinnige Aktiveer/Deaktiveer)

Redigeer `appsettings.json` → `FeatureFlags`-afdeling:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 Aktiveer vir verifikasie
  "EnableNonceServices": false,  // 🔒 Aktiveer vir CSP
  "EnableCosmosDb": false,       // 💾 Aktiveer vir databasis
  "EnableBlobStorage": false     // 📎 Aktiveer vir lêers
}
```

## 🔑 Gebruikersgeheime-Opdragte

```powershell
# Initialiseer
dotnet user-secrets init

# Stel 'n geheim
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# Lys alle geheime
dotnet user-secrets list

# Verwyder 'n geheim
dotnet user-secrets remove "AzureAd:ClientSecret"

# Verwyder alle geheime
dotnet user-secrets clear
```

## 🔐 Vereiste Geheime per Funksie

### Azure AD-Verifikasie
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Genereer eers: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## 🛠️ Nuttige Skrifte

| Skrif | Doel | Gebruik |
|-------|------|---------|
| `SetupFromTemplate.ps1` | Aanvanklike opstel | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Verander naamruimte | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Genereer sleutels | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Bereken CSP-hashes | `.\HashInlineScriptPowerShell.ps1` |

## 🗺️ Ontwikkelingsfases

### Fase 1: Basiese (5 min opstel)
- ✅ Sessie
- ✅ Lokalisering
- ✅ Sekuriteitsopskrifte
- ❌ Geen verifikasie
- ❌ Geen databasis

**Konfigurasie**: Alle vlaggies `false` behalwe `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Fase 2: + Verifikasie (30 min opstel)
- ✅ Fase 1-funksies
- ✅ Azure AD
- ✅ Magtiging
- ✅ CSP + Nonce
- ❌ Geen databasis

**Konfigurasie**: Aktiveer `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

**Vereis**:
- Azure AD-toepassingsregistrasie
- Gegenereerde enkripsiesleutels

### Fase 3: + Azure-Dienste (1–2 uur opstel)
- ✅ Fase 2-funksies
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**Konfigurasie**: Aktiveer `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

**Vereis**:
- Azure-hulpbronne geskep
- Verbindingstrings in Gebruikersgeheime

## 🔍 Vinnige Probleemoplossing

### Boufoute
```powershell
# Maak skoon en herbou
dotnet clean
dotnet build

# Kontroleer vir ontbrekende pakkette
dotnet restore
```

### "Configuration not found"
```powershell
# Verifieer dat lêer bestaan
Test-Path appsettings.json

# As dit ontbreek, kopieer van sjabloon
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# Lys geheime
dotnet user-secrets list

# Hervoer opstel
.\SupportingScripts\SetupFromTemplate.ps1
```

### Verifikasielus / 401-foute
1. Kontroleer of Azure AD-herleidings-URI ooreenstem
2. Verifieer `EnableAzureAd: true` in appsettings.json
3. Kontroleer kliëntgeheim in Gebruikersgeheime
4. Maak blaaierkoekies skoon

### CSP-skendinge
1. Verifieer `EnableNonceServices: true`
2. Kontroleer of enkripsiesleutels gestel is
3. Hersien blaaier-konsole vir CSP-foute
4. Deaktiveer CSP tydelik om te toets: `EnableCSP: false`

## 📚 Dokumentasie

- **Volledige dokumente**: `TEMPLATE_README.md`
- **Konfigurasie**: `appsettings.template.json`
- **Naamruimte**: Voer `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"` uit

## 🔒 Sekuriteitskontrolelys

Voor ontplooiing na produksie:

- [ ] Alle geheime in Azure Key Vault of Gebruikersgeheime
- [ ] `appsettings.json` is git-geïgnoreer
- [ ] `.gitignore` sluit sjabloon-spesifieke ignorerings in
- [ ] Sekuriteitsopskrifte geaktiveer
- [ ] CSP met nonces gekonfigureer
- [ ] HTTPS afgedwing
- [ ] Verifikasie geaktiveer vir beskermde bladsye
- [ ] Geheime van standaardwaardes geroteer

## 💡 Wenke

- **Begin eenvoudig**: Begin met Fase 1, voeg funksies stapsgewys by
- **Gebruik WhatIf**: Toets skrifte met `-WhatIf` voor toepassing
- **Kontroleer logs**: Aktiveer `"Default": "Debug"` in `Logging:LogLevel` vir probleemoplossing
- **Verifieer geheime**: Voer `dotnet user-secrets list` uit om te sien wat gekonfigureer is
- **Skoon bouers**: As vreemde foute, probeer `dotnet clean && dotnet build`

## ❓ Hulp

1. Lees `TEMPLATE_README.md`
2. Kontroleer `appsettings.template.json`-kommentare
3. Voer `dotnet user-secrets list` uit
4. Aktiveer foutopsporingaantekening
5. Kontroleer Azure-portaal vir hulpbronstatus

---

**Sjabloonweergawe**: 1.0  
**ASP.NET Core**: 9.0  
**Laas Opgedateer**: 2024-12-20
