# Treoir mTLS (Mutual TLS) do Fhíordheimhniú Deimhnithe Cliaint

## Forbhreathnú

Tacaíonn an tionscadal seo anois le fíordheimhniú **mutual TLS (mTLS)**, a éilíonn go gcuireann an freastalaí agus an cliant araon deimhnithe bailí i láthair. Cuireann sé seo slándáil fheabhsaithe ar fáil trí fhíordheimhniú dhá-threo.

## Cad is mTLS ann?

Leathnaíonn mTLS TLS caighdeánach trí:
1. **Deimhniú Freastalaí**: cuireann an freastalaí deimhniú i láthair chun a chéannacht a chruthú (HTTPS caighdeánach)
2. **Deimhniú Cliant**: cuireann an cliant deimhniú i láthair freisin chun a chéannacht a chruthú (an chuid bhreise mTLS)

## Cumraíocht

### 1. Bratach Ghné

Cumasaigh mTLS in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Socruithe mTLS

Cumraigh iompraíocht mTLS in `appsettings.json`:

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

#### Roghanna Cumraíochta

| Socrú | Cineál | Réamhshocrú | Cur síos |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | Má tá true ann, tá deimhniú cliant éigeantach |
| `AllowCertificateChains` | bool | `true` | Ceadaigh deimhnithe slabhraithe (sínithe ag CA) |
| `AllowSelfSignedCertificates` | bool | `false` | Ceadaigh deimhnithe féin-shínithe (forbairt amháin) |
| `CheckCertificateRevocation` | bool | `false` | Déan seiceáil cúlghairme ar líne |
| `ClientCertificateName` | string | null | Ainm deimhnithe in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Bailíochtaigh eisitheoir an deimhnithe |

### 3. Deimhniú Freastalaí (Azure Key Vault)

Faightear deimhniú an fhreastalaí ó Azure Key Vault:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Treoracha Socraithe

### Réamhriachtanais

1. Azure Key Vault le ceadanna cuí
2. Deimhniú freastalaí stóráilte in Azure Key Vault mar rún (formáid PFX)
3. Deimhnithe cliaint (ginithe nó faighte ó CA)

### Céim 1: Uaslódáil Deimhniú Freastalaí go Key Vault

```bash
# Tiontaigh deimhniú go PFX más gá
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Uaslódáil go Key Vault le Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Stóráil pasfhocal mar rún ar leith
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Céim 2: Gin Deimhnithe Cliant

#### Rogha A: Féin-shínithe (Forbairt amháin)

```powershell
# Gin deimhniú cliaint
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Easpórtáil go PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Rogha B: Sínithe ag CA (Táirgeadh)

Oibrigh le d'Údarás Deimhnithe chun deimhnithe cliaint a fháil.

### Céim 3: Cumraigh an Feidhmchlár

Nuashonraigh `appsettings.json`:

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

### Céim 4: Tástáil le Deimhniú Cliant

#### Ag úsáid cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### Ag úsáid PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Ag úsáid Brabhsálaí:

1. Iompórtáil deimhniú cliaint go stór deimhnithe an bhrabhsálaí
2. Seol chuig an bhfeidhmchlár
3. Iarrfar ort deimhniú cliaint a roghnú

## Iompraíocht de réir Timpeallachta

### Forbairt
- Luchtaítear deimhniú freastalaí ó Key Vault (má tá ar fáil)
- Tá deimhnithe cliaint **roghnach** (`AllowCertificate` mode)
- Is féidir deimhnithe féin-shínithe a cheadú

### Táirgeadh
- Luchtaítear deimhniú freastalaí ó Key Vault
- Tá deimhnithe cliaint **riachtanach** má tá `EnableMtls = true`
- Moltar deimhnithe slabhraithe amháin

## Dea-Chleachtais Slándála

### ✅ DÉAN:
- Úsáid deimhnithe sínithe ag CA i dtáirgeadh
- Stóráil deimhnithe in Azure Key Vault
- Cumasaigh seiceáil cúlghairme i dtáirgeadh
- Bailíochtaigh eisitheoir deimhnithe
- Úsáid pasfhocail láidre do chomhaid PFX
- Rothlaigh deimhnithe go rialta

### ❌ NÁ DÉAN:
- Ná húsáid deimhnithe féin-shínithe i dtáirgeadh
- Ná geall deimhnithe sa rialú foinse
- Ná roinn deimhnithe cliaint idir úsáideoirí
- Ná díchumasaigh bailíochtú deimhnithe i dtáirgeadh

## Fabhtcheartú

### Earráid: "No client certificate provided"

**Cúis**: Níor sheol an cliant deimhniú  
**Réiteach**:
- Deimhnigh go bhfuil deimhniú cliaint suiteáilte
- Seiceáil socrú `RequireClientCertificate`
- Cinntigh go bhfuil an deimhniú iontaofa ag an gcóras

### Earráid: "Certificate chain validation failed"

**Cúis**: Níl deimhniú iontaofa  
**Réiteach**:
- Suiteáil fréamh-dheimhniú an CA
- Socraigh `AllowSelfSignedCertificates = true` le haghaidh tástála
- Deimhnigh nach bhfuil an deimhniú as dáta

### Earráid: "Server certificate not retrieved from Key Vault"

**Cúis**: Fadhb rochtana Azure Key Vault  
**Réiteach**:
- Seiceáil ceadanna Key Vault
- Seiceáil dintiúir Azure AD
- Cinntigh go bhfuil managed identity cumraithe

## Logáil

Déantar imeachtaí fíordheimhnithe mTLS a logáil:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Comhtháthú le Fíordheimhniú Reatha

Oibríonn mTLS in éineacht le fíordheimhniú Azure AD:

1. **Bailíochtú Deimhnithe Cliant** ar dtús (sraith iompair)
2. **Fíordheimhniú Azure AD** ina dhiaidh sin (sraith feidhmchláir)

Is féidir an dá cheann a chumasú le chéile le haghaidh cosanta iolraí.

## Tagairtí

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Sampla Cód

Is féidir an cur i bhfeidhm a fháil i:
- `Models/Settings/MtlsSettings.cs` - múnla cumraíochta
- `Models/Settings/FeatureFlags.cs` - bratach gné
- `Extensions/ServiceCollectionExtensions.cs` - clárú seirbhíse
- `Program.cs` - tosú an fheidhmchláir

## Acmhainní Breise

Féach `SupportingScripts/CertificateUploaderToAzureExample.ps1` le haghaidh samplaí uaslódála deimhnithe.
