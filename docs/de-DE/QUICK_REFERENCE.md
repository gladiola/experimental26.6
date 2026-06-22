# Kurzübersicht — Razor Pages Template

## Schnellstart (5 Minuten)

```powershell
# 1) Setup-Skript ausführen
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2) Build & Run
dotnet build
dotnet run
```

## Konfigurationsdateien

| Datei | Zweck | Eingecheckt? |
|---|---|---|
| `appsettings.template.json` | Vorlage mit Platzhaltern | ✅ Ja |
| `appsettings.json` | Ihre echte Konfiguration | ❌ Nein (git-ignored) |
| User Secrets | Sensible Werte | ❌ Nein (lokal) |

## Feature Flags (schnell ein-/ausschalten)

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## User-Secrets-Befehle

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## Erforderliche Secrets nach Feature

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Erst erzeugen: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## Nützliche Skripte

| Skript | Zweck | Nutzung |
|---|---|---|
| `SetupFromTemplate.ps1` | Initiales Setup | `.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Namespace ändern | `.\SupportingScripts\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Schlüssel erzeugen | `.\SupportingScripts\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | CSP-Hashes berechnen | `.\SupportingScripts\HashInlineScriptPowerShell.ps1` |

## Entwicklungsphasen

### Phase 1: Basis
- Session
- Lokalisierung
- Security Header
- ohne Auth, ohne DB

### Phase 2: + Auth
- Phase 1 + Azure AD + Autorisierung + CSP/Nonce

### Phase 3: + Azure Services
- Phase 2 + Cosmos DB + Blob Storage + Key Vault

## Schnelle Fehlerbehebung

### Build-Fehler
```powershell
dotnet clean
dotnet restore
dotnet build
```

### „Configuration not found“
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### „Secret not found“
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth-Loop / 401
1. Redirect-URI in Azure AD prüfen
2. `EnableAzureAd: true` prüfen
3. Client Secret in User Secrets prüfen
4. Browser-Cookies löschen

### CSP-Verstöße
1. `EnableNonceServices: true` prüfen
2. Key/IV gesetzt?
3. Browser-Konsole prüfen
4. testweise `EnableCSP: false`

## Security-Checkliste (Produktion)

- [ ] Secrets nur in Key Vault/User Secrets
- [ ] `appsettings.json` ist git-ignored
- [ ] Security Header aktiv
- [ ] CSP mit Nonces aktiv
- [ ] HTTPS erzwungen
- [ ] Authentifizierung für geschützte Seiten aktiv
- [ ] Secrets rotiert

---

**Template-Version:** 1.0  
**ASP.NET Core:** 9.0  
**Zuletzt aktualisiert:** 2024-12-20
