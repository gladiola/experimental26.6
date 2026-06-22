# Leitfaden: Azure Key Vault PFX-Zertifikate

## Datum: 2024-12-20

## Überblick

Dieser Leitfaden beschreibt den **korrekten Ansatz** zum Speichern und Abrufen vollständiger PFX-Zertifikate (inkl. privatem Schlüssel) in Azure Key Vault.

---

## Häufige Fehler vermeiden

### FALSCH: PFX als Base64-Secret speichern

```powershell
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Warum das fehlschlägt:**
1. Secret-Größenlimit (25 KB)
2. Kodierungs-/Korruptionsrisiken
3. Typ-Missmatch (String-Secret statt Zertifikatsobjekt)
4. Verlust von Zertifikatsmetadaten

---

## RICHTIG: Zertifikats-APIs verwenden

### Methode 1: Zertifikat direkt importieren (empfohlen)

```powershell
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Vorteile:**
- unterstützt große Zertifikate
- behält Metadaten
- erzeugt Secret-Version mit privatem Schlüssel
- unterstützt Rotation
- integriert in RBAC/Policies

### Abruf in C#

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public async Task<X509Certificate2?> GetCertificateFromKeyVaultAsync(
    string tenantId,
    string clientId,
    string clientSecret,
    string keyVaultUrl,
    string certificateName)
{
    try
    {
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
    }
    catch (Exception)
    {
        return null;
    }
}
```

---

### Methode 2: Managed Identity (Produktion)

In produktiven Azure-Umgebungen `DefaultAzureCredential`/Managed Identity bevorzugen.

```csharp
var credential = new DefaultAzureCredential();
```

---

## Stand in WebAppExperimental266

**Ort:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`  
**Status:** Template-Implementierung; produktionsreifer Code erforderlich.

---

## Erforderliche NuGet-Pakete

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

Hinweis: Bereits im Projekt vorhanden.

---

## Konfiguration

### appsettings.json

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
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "ClientCertificateName": "client-cert"
  }
}
```

### User Secrets

```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"
```

---

## Berechtigungen in Azure Key Vault

Für Service Principal oder Managed Identity:

- **Certificate Permissions:** Get, List
- **Secret Permissions:** Get, List

Beides ist erforderlich: Zertifikat-Metadaten + privater Schlüssel.

---

## Testen

### Unit-Test (Beispiel)

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);
    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");

    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
}
```

---

## Einsatz mit mTLS

Wenn `EnableMtls` und `EnableKeyVault` aktiv sind, kann das Serverzertifikat beim Start aus Key Vault geladen und in Kestrel konfiguriert werden.

---

## Vergleich: Secret vs. Certificate

| Merkmal | Als Secret | Als Certificate |
|---|---|---|
| Größenlimit | 25 KB | praktisch unbegrenzt |
| Privater Schlüssel | manuell | automatisch |
| Metadaten | keine | vollständig |
| Rotation | manuell | integriert |
| Empfehlung | nicht empfohlen | **empfohlen** |

---

## Rotation

Auto-Rotation über Key-Vault-Policy aktivierbar (z. B. 30 Tage vor Ablauf).

---

## Troubleshooting

### „Certificate not found“
- Name prüfen
- Existenz in Key Vault prüfen
- Berechtigungen prüfen

### „Access denied“
- Principal-Berechtigungen prüfen
- Certificate + Secret Rechte vorhanden?

### „Certificate has no private key“
- Nicht nur `GetCertificateAsync`, sondern auch Secret abrufen

### „CryptographicException“
- PFX-Daten/Format prüfen

---

## Migrations-Checkliste

- [ ] Benötigte NuGet-Pakete installiert
- [ ] Produktive Implementierung in `AzureKeyVaultCertificateOperations.cs`
- [ ] Zertifikat via `Import-AzKeyVaultCertificate` importiert
- [ ] Access Policies gesetzt (Cert + Secret Get/List)
- [ ] Konfiguration aktualisiert
- [ ] Managed Identity (Prod) oder Client Secret (Dev) konfiguriert
- [ ] Zertifikatabruf getestet
- [ ] Vorhandensein des privaten Schlüssels verifiziert
- [ ] mTLS-End-to-End getestet
- [ ] Rotationsstrategie dokumentiert

---

## Kurzfazit

### Do
- `Import-AzKeyVaultCertificate` verwenden
- `CertificateClient` + `SecretClient` kombinieren
- in Produktion Managed Identity nutzen
- Cert- und Secret-Berechtigungen vergeben

### Don’t
- PFX als Base64-Secret speichern
- Zertifikatsdaten manuell „zusammenbauen"
- Produktionsbetrieb mit Client Secret, wenn Managed Identity möglich ist

---

**Status:** Leitfaden vollständig  
**Zuletzt aktualisiert:** 2024-12-20  
**Version:** 1.0  
**Projekt:** WebAppExperimental266
