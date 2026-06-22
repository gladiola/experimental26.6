# mTLS (Mutual TLS) — Leitfaden zur Client-Zertifikat-Authentifizierung

## Überblick

Dieses Projekt unterstützt **Mutual TLS (mTLS)**: Server **und** Client weisen sich mit Zertifikaten aus. Dadurch entsteht eine starke Zwei-Wege-Authentifizierung.

## Was ist mTLS?

mTLS erweitert TLS um ein Client-Zertifikat:
1. **Serverzertifikat** (Standard-HTTPS)
2. **Clientzertifikat** (zusätzliche mTLS-Prüfung)

## Konfiguration

### 1. Feature-Flag

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. mTLS-Einstellungen

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

| Einstellung | Typ | Standard | Beschreibung |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | Client-Zertifikat zwingend erforderlich |
| `AllowCertificateChains` | bool | `true` | CA-signierte Zertifikatsketten zulassen |
| `AllowSelfSignedCertificates` | bool | `false` | Selbstsignierte Zertifikate zulassen (nur Dev) |
| `CheckCertificateRevocation` | bool | `false` | Online-Sperrprüfung durchführen |
| `ClientCertificateName` | string | null | Zertifikatsname in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Issuer-Prüfung aktivieren |

### 3. Serverzertifikat (Azure Key Vault)

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Setup

### Voraussetzungen

1. Azure Key Vault mit passenden Berechtigungen
2. Serverzertifikat (PFX) im Key Vault
3. Client-Zertifikate (selbst erzeugt oder von CA)

### Schritt 1: Serverzertifikat hochladen

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Schritt 2: Client-Zertifikate erstellen

#### Option A: Selbstsigniert (nur Entwicklung)

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

#### Option B: CA-signiert (Produktion)

Mit Ihrer Zertifizierungsstelle (CA) ausstellen lassen.

### Schritt 3: App konfigurieren

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### Schritt 4: Testen

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

## Umgebungsverhalten

### Entwicklung
- Serverzertifikat aus Key Vault (falls verfügbar)
- Client-Zertifikat optional (`AllowCertificate`)
- Selbstsignierte Zertifikate optional

### Produktion
- Serverzertifikat aus Key Vault
- Client-Zertifikat erforderlich bei `EnableMtls = true`
- Bevorzugt CA-signierte Ketten

## Sicherheits-Best-Practices

### Do
- CA-signierte Zertifikate in Produktion
- Zertifikate in Key Vault speichern
- Sperrprüfung in Produktion aktivieren
- Issuer validieren
- Zertifikate regelmäßig rotieren

### Don’t
- Keine selbstsignierten Zertifikate in Produktion
- Zertifikate nie ins Repo committen
- Kein Teilen von Client-Zertifikaten über mehrere Nutzer

## Fehlerbehebung

### „No client certificate provided“
- Zertifikat korrekt installiert?
- `RequireClientCertificate` korrekt gesetzt?
- Zertifikat auf Systemebene vertrauenswürdig?

### „Certificate chain validation failed“
- CA-Root installiert?
- Für Tests ggf. `AllowSelfSignedCertificates = true`
- Ablaufdatum prüfen

### „Server certificate not retrieved from Key Vault“
- Key-Vault-Berechtigungen prüfen
- Azure-AD-Zugangsdaten prüfen
- Managed Identity korrekt konfiguriert?

## Logging (Beispiele)

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Zusammenspiel mit bestehender Authentifizierung

mTLS und Azure AD funktionieren gemeinsam:
1. Zertifikatsprüfung auf Transportebene
2. Azure AD auf Anwendungsebene

## Referenzen

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Code-Orte

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`
