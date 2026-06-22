# Guida certificati PFX in Azure Key Vault

## Data: 2024-12-20

## Panoramica

Questa guida descrive l'approccio corretto per archiviare e recuperare certificati PFX completi (con chiave privata) in Azure Key Vault.

## Errori comuni da evitare

### Sbagliato: salvare il PFX come segreto Base64

Questo approccio fallisce spesso per:
1. Limite dimensione segreti (25 KB)
2. Problemi di encoding/corruzione
3. Disallineamento tipo dati (stringa vs certificato binario)
4. Perdita metadati del certificato

## Approccio corretto: API certificate-aware

### Metodo 1: Import diretto certificato (consigliato)

```powershell
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\\path\\to\\your\\certificate.pfx"
$plainPassword = "your-pfx-password"

$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

Vantaggi:
- Gestione nativa certificati
- Metadati preservati
- Versioning/rotazione supportati
- Integrazione con RBAC/policy Azure

### Recupero certificato (C#)

```csharp
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
KeyVaultCertificateWithPolicy certificate = await certificateClient.GetCertificateAsync(certificateName);

var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

byte[] pfxBytes = Convert.FromBase64String(secret.Value);
return new X509Certificate2(
    pfxBytes,
    (string?)null,
    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
```

### Metodo 2: Managed Identity (produzione)

Preferisci `DefaultAzureCredential` in Azure:

```csharp
var credential = new DefaultAzureCredential();
```

## Stato in WebAppExperimental266

- **Posizione:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`
- **Stato:** implementazione template; da sostituire con codice production-ready

## Pacchetti NuGet richiesti

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

## Configurazione

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "{{USE_USER_SECRETS}}",
    "KeyVaultPassName": "server-cert"
  }
}
```

## Permessi Key Vault necessari

Per l'identità applicativa:

- **Certificate permissions:** Get, List
- **Secret permissions:** Get, List

Servono entrambi perché:
- le API certificato forniscono metadati
- il segreto contiene la chiave privata

## Troubleshooting

### "Certificate not found"
1. Nome certificato corretto
2. Certificato presente in Key Vault
3. Policy/permessi configurati

### "Access denied"
1. Permessi assegnati al principal corretto
2. Concessi sia certificate che secret permissions
3. Managed Identity abilitata (se usata)

### "Certificate has no private key"
Controlla di usare sia `GetCertificateAsync` sia `GetSecretAsync`.

### `CryptographicException`
Cause tipiche:
1. Dati PFX corrotti
2. Formato non valido
3. Versione/valore segreto errato

## Checklist migrazione

- [ ] Verifica pacchetti NuGet
- [ ] Aggiorna `AzureKeyVaultCertificateOperations.cs`
- [ ] Importa certificato con `Import-AzKeyVaultCertificate`
- [ ] Configura permessi Get/List per Certificate e Secret
- [ ] Aggiorna `appsettings.json`
- [ ] Configura Managed Identity (prod) o client secret (dev)
- [ ] Verifica recupero certificato con chiave privata
- [ ] Testa integrazione mTLS

## Riepilogo

### FARE
- Import certificati con API certificate-aware
- Recuperare metadati + segreto associato
- Usare Managed Identity in produzione
- Testare sempre presenza chiave privata

### NON FARE
- Salvare PFX come segreto Base64 generico
- Gestire manualmente parsing/encoding quando evitabile
- Dimenticare permessi Secret oltre ai Certificate

---

**Stato:** Guida completa  
**Ultimo aggiornamento:** 2024-12-20  
**Versione:** 1.0
