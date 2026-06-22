# mTLS (Mutual TLS) क्लाइंट प्रमाणपत्र प्रमाणीकरण मार्गदर्शिका

## अवलोकन

यह परियोजना अब **mTLS** का समर्थन करती है, जिसमें सर्वर और क्लाइंट दोनों वैध प्रमाणपत्र प्रस्तुत करते हैं।

## mTLS क्या है?

मानक TLS के ऊपर mTLS दो-तरफ़ा पहचान सुनिश्चित करता है:
1. **Server Certificate** (HTTPS)
2. **Client Certificate** (mTLS)

## कॉन्फ़िगरेशन

### 1) Feature Flag

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2) mTLS Settings

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

| Setting | Type | Default | विवरण |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | क्लाइंट प्रमाणपत्र अनिवार्य |
| `AllowCertificateChains` | bool | `true` | CA-signed chain स्वीकार |
| `AllowSelfSignedCertificates` | bool | `false` | self-signed (dev) |
| `CheckCertificateRevocation` | bool | `false` | revocation check |
| `ClientCertificateName` | string | null | Key Vault cert नाम |
| `ValidateClientCertificateIssuer` | bool | `true` | issuer सत्यापन |

### 3) Server Certificate (Azure Key Vault)

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## सेटअप चरण

1. Key Vault + permissions तैयार करें
2. सर्वर cert (PFX) Vault में रखें
3. क्लाइंट cert जनरेट/प्राप्त करें

### Step 1: Server cert upload

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Step 2: Client cert

#### विकल्प A: Self-signed (केवल development)

```powershell
$cert = New-SelfSignedCertificate -Subject "CN=MyClient" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256 -NotAfter (Get-Date).AddYears(2)
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### विकल्प B: CA-signed (production)

अपनी Certificate Authority के साथ client certificates जारी करें।

### Step 3: एप्लिकेशन कॉन्फ़िगर करें

`appsettings.json` में KeyVault + mTLS settings सक्षम करें।

### Step 4: परीक्षण

**cURL**
```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

**PowerShell**
```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

**Browser**
1. cert import करें  
2. साइट खोलें  
3. client cert चुनें

## वातावरण-विशिष्ट व्यवहार

### Development
- client cert वैकल्पिक हो सकते हैं
- self-signed स्वीकार्य (यदि सक्षम)

### Production
- `EnableMtls = true` पर client cert आवश्यक
- CA-signed cert अनुशंसित

## सुरक्षा सर्वोत्तम प्रथाएँ

### करें
- production में CA-signed cert
- Key Vault में cert संग्रह
- revocation + issuer validation सक्षम करें
- cert rotation अपनाएँ

### न करें
- production में self-signed cert
- cert source control में commit
- validation disable करना

## Troubleshooting

### "No client certificate provided"
- cert इंस्टॉल है या नहीं
- `RequireClientCertificate` जांचें

### "Certificate chain validation failed"
- CA root इंस्टॉल करें
- परीक्षण में अस्थायी रूप से `AllowSelfSignedCertificates=true`

### "Server certificate not retrieved from Key Vault"
- Vault permission / identity / Azure AD सेटिंग जांचें

## Logging

उदाहरण:

```text
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## मौजूदा प्रमाणीकरण के साथ एकीकरण

mTLS (transport layer) और Azure AD (application layer) एक साथ चल सकते हैं।

## संदर्भ

- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth
- https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code

## अतिरिक्त संसाधन

`SupportingScripts/CertificateUploaderToAzureExample.ps1`
