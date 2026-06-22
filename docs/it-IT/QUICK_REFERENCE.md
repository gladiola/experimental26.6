# Scheda di Riferimento Rapido - Template Razor Pages

## Avvio rapido (5 minuti)

```powershell
# 1. Esegui lo script di setup
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build & run
dotnet build
dotnet run
```

## File di configurazione

| File | Scopo | Versionato? |
|------|-------|-------------|
| `appsettings.template.json` | Template con segnaposto | Sì |
| `appsettings.json` | Configurazione reale | No (git-ignored) |
| User Secrets | Valori sensibili | No (solo locale) |

## Feature flag (abilita/disabilita velocemente)

Modifica la sezione `FeatureFlags` in `appsettings.json`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## Comandi User Secrets

```powershell
# Inizializza
dotnet user-secrets init

# Imposta un segreto
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# Elenca tutti i segreti
dotnet user-secrets list

# Rimuovi un segreto
dotnet user-secrets remove "AzureAd:ClientSecret"

# Pulisci tutti i segreti
dotnet user-secrets clear
```

## Segreti richiesti per funzionalità

### Autenticazione Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Genera prima: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## Script utili

| Script | Scopo | Uso |
|--------|-------|-----|
| `SetupFromTemplate.ps1` | Setup iniziale | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Cambiare namespace | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Generare chiavi | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Calcolare hash CSP | `.\HashInlineScriptPowerShell.ps1` |

## Fasi di sviluppo

### Fase 1: Base
- Sessione
- Localizzazione
- Header di sicurezza
- Senza auth
- Senza database

### Fase 2: + Autenticazione
- Funzionalità fase 1
- Azure AD
- Authorization
- CSP + Nonce

### Fase 3: + Servizi Azure
- Funzionalità fase 2
- Cosmos DB
- Blob Storage
- Key Vault

## Troubleshooting rapido

### Errori di build
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

### Loop auth / errori 401
1. Verifica redirect URI su Azure AD
2. Verifica `EnableAzureAd: true`
3. Controlla client secret in User Secrets
4. Svuota i cookie del browser

### Violazioni CSP
1. Verifica `EnableNonceServices: true`
2. Verifica che chiavi e IV siano configurati
3. Controlla la console del browser
4. Test temporaneo: `EnableCSP: false`

## Checklist sicurezza

Prima del deploy in produzione:

- [ ] Tutti i segreti in User Secrets o Key Vault
- [ ] `appsettings.json` non versionato
- [ ] Header di sicurezza attivi
- [ ] CSP con nonce configurata
- [ ] HTTPS obbligatorio
- [ ] Autenticazione attiva sulle pagine protette
- [ ] Segreti ruotati

---

**Versione template**: 1.0  
**ASP.NET Core**: 9.0  
**Ultimo aggiornamento**: 2024-12-20
