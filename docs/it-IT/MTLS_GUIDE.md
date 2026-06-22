# Guida all'autenticazione con certificato client mTLS (Mutual TLS)

## Panoramica

Questo progetto supporta l'autenticazione **mTLS**, che richiede certificati validi sia lato server sia lato client, per una sicurezza a doppio fattore a livello di trasporto.

## Cos'è mTLS?

mTLS estende TLS standard richiedendo:
1. **Certificato server**: identità del server (HTTPS standard)
2. **Certificato client**: identità del client (estensione mTLS)

## Configurazione

### 1. Feature flag

Abilita mTLS in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Impostazioni mTLS

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

| Impostazione | Tipo | Default | Descrizione |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | Certificato client obbligatorio |
| `AllowCertificateChains` | bool | `true` | Consente certificati concatenati (firmati CA) |
| `AllowSelfSignedCertificates` | bool | `false` | Consente certificati self-signed (solo dev) |
| `CheckCertificateRevocation` | bool | `false` | Verifica online revoca certificato |
| `ClientCertificateName` | string | null | Nome certificato in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Valida issuer certificato |

### 3. Certificato server (Azure Key Vault)

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Istruzioni di setup

### Prerequisiti
1. Azure Key Vault con permessi adeguati
2. Certificato server in Key Vault (PFX)
3. Certificati client (generati o rilasciati da CA)

### Step 1: Carica il certificato server in Key Vault

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Step 2: Genera certificati client

#### Opzione A: Self-signed (solo sviluppo)

```powershell
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Opzione B: CA-signed (produzione)

Lavora con la tua Certification Authority per ottenere i certificati client.

### Step 3: Configura l'applicazione

Aggiorna `appsettings.json` con i valori effettivi di Key Vault e mTLS.

### Step 4: Test con certificato client

#### cURL
```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### PowerShell
```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Browser
1. Importa il certificato client nello store del browser
2. Apri l'applicazione
3. Seleziona il certificato quando richiesto

## Comportamento per ambiente

### Sviluppo
- Certificato server da Key Vault (se disponibile)
- Certificati client opzionali (`AllowCertificate`)
- Self-signed eventualmente consentiti

### Produzione
- Certificato server da Key Vault
- Certificati client obbligatori se `EnableMtls = true`
- Raccomandati certificati concatenati (CA-signed)

## Best practice di sicurezza

### FARE
- Usare certificati CA-signed in produzione
- Archiviare certificati in Key Vault
- Abilitare revoca certificati in produzione
- Validare issuer
- Ruotare periodicamente i certificati

### NON FARE
- Usare self-signed in produzione
- Versionare certificati nel repository
- Condividere lo stesso certificato client tra utenti
- Disabilitare la validazione in produzione

## Troubleshooting

### Errore: "No client certificate provided"
- Verifica installazione certificato client
- Controlla `RequireClientCertificate`
- Verifica trust chain del certificato

### Errore: "Certificate chain validation failed"
- Installa il certificato root CA
- Per test: `AllowSelfSignedCertificates = true`
- Verifica scadenza del certificato

### Errore: "Server certificate not retrieved from Key Vault"
- Verifica permessi Key Vault
- Controlla credenziali Azure AD
- Verifica Managed Identity

## Integrazione con autenticazione esistente

mTLS può convivere con Azure AD:
1. Validazione certificato client (transport layer)
2. Autenticazione Azure AD (application layer)

## Riferimenti

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Codice rilevante

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`
