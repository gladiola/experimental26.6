# Treoir Cur i bhFeidhm OCSP (Online Certificate Status Protocol)

## Forbhreathnú

Tá **tacaíocht teimpléid** san áireamh sa tionscadal seo do bhailíochtú deimhnithe OCSP. Ceadaíonn OCSP stádas cúlghairme deimhnithe a sheiceáil i bhfíor-am sula bpróiseáiltear iarratais ghréasáin.

## Cad is OCSP ann?

Soláthraíonn OCSP rogha eile seachas CRLanna chun seiceáil an bhfuil deimhniú cúlghairthe:

- **Bailíochtú fíor-ama**: seiceálann stádas láithreach
- **Éifeachtúil**: fiosraíonn sé deimhnithe ar leith amháin
- **Éadrom**: freagraí níos lú ná íoslódálacha CRL iomlána
- **Nuashonraithe**: eolas cúlghairme reatha i gcónaí

## Cumraíocht

### 1. Bratach Gné

Cumasaigh OCSP in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Socruithe OCSP

Cumraigh iompraíocht OCSP in `appsettings.json`:

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

### Roghanna Cumraíochta

| Socrú | Cineál | Réamhshocrú | Cur síos |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | Cumasaigh/díchumasaigh bailíochtú OCSP |
| `OcspServerUrl` | string | `null` | URL do fhreastalaí freagróra OCSP |
| `RequestTimeoutSeconds` | int | `30` | Teorainn ama d'iarratais OCSP |
| `MaxRetryAttempts` | int | `3` | Líon iarrachtaí athdhéanaimh |
| `CacheDurationMinutes` | int | `60` | Fad ama chun freagraí OCSP a chur i dtaisce |
| `ServerUnavailableBehavior` | string | `"Warn"` | Iompar má tá an freastalaí síos: `"Fail"`, `"Allow"`, nó `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Cumasaigh logáil mhionsonraithe |
| `SkipValidationInDevelopment` | bool | `true` | Scipeáil OCSP i mód forbartha |

---

## Cur i bhFeidhm Teimpléid

Is **teimpléad** é an cur i bhfeidhm reatha a léiríonn struchtúr agus dearadh API. Chun OCSP a úsáid i dtáirgeadh, caithfidh tú:

### 1. Prótacal OCSP a chur i bhfeidhm

Cuir cur i bhfeidhm fíor in áit `PerformOcspValidationAsync` in `OcspValidationService.cs`:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: Implement actual OCSP protocol
    // 1. Build OCSP request
    // 2. Send to OCSP server
    // 3. Parse OCSP response
    // 4. Validate response signature
    // 5. Return certificate status
}
```

### 2. Freastalaí OCSP a thógáil

Teastaíonn freastalaí freagróra OCSP ar leith a:
- Glacann le hiarratais OCSP (formáid RFC 6960)
- Seiceálann stádas deimhnithe i mbunachar an CA
- Filleann freagraí OCSP sínithe

**Roghanna:**
- Seirbhís tráchtála OCSP (m.sh. DigiCert, Let's Encrypt)
- Freagróir saincheaptha le leabharlanna:
  - **OpenSSL**
  - **BouncyCastle** (.NET)
  - **Python** `cryptography` le tacaíocht OCSP

---

## Sampla Úsáide

### Bailíochtú Bunúsach Deimhnithe

```csharp
public class MyCertificateHandler
{
    private readonly IOcspValidationService _ocspService;

    public MyCertificateHandler(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Bailíochtú Mionsonraithe Stádais

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Certificate is valid");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Certificate has been revoked!");
            throw new SecurityException("Certificate revoked");

        case OcspStatus.Unknown:
            logger.LogWarning("Certificate status unknown");
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            break;
    }

    return result;
}
```

---

## Comhtháthú le mTLS

Oibríonn OCSP go díreach le fíordheimhniú deimhnithe mTLS:

```csharp
// In ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// In certificate validation event
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Certificate validation failed via OCSP");
        }
    }
};
```

---

## Iompraíochtaí nuair nach bhfuil an Freastalaí ar Fáil

### "Fail" — Slándáil Dhian

```json
"ServerUnavailableBehavior": "Fail"
```

- Diúltaíonn iarratais má tá freastalaí OCSP síos
- An rogha is sláine
- D'fhéadfadh infhaighteacht a laghdú

### "Allow" — Ard-Infhaighteacht

```json
"ServerUnavailableBehavior": "Allow"
```

- Ceadaíonn iarratais nuair atá an freastalaí síos
- Tosaíocht don infhaighteacht thar slándáil
- Logálann rabhaidh

### "Warn" — Cothromaithe (Réamhshocrú)

```json
"ServerUnavailableBehavior": "Warn"
```

- Ceadaíonn iarratais ach logálann rabhaidh
- Cur chuige cothromaithe
- Cabhraíonn le monatóireacht/aláraim

---

## Caching

Cuirtear freagraí OCSP i dtaisce chun ualach freastalaí a laghdú:

```json
"CacheDurationMinutes": 60
```

**Buntáistí:**
- Laghdaíonn fiosrúcháin chuig freastalaí OCSP
- Feabhsaíonn feidhmíocht
- Soláthraíonn athléimneacht le linn bristeacha gearra

**Neamhbhailíochtú taisce:**
- Uathoibríoch tar éis don tréimhse taisce dul in éag
- Glanadh láimhe: atosú an feidhmchlár

---

## Breithnithe Slándála

### ✅ DÉAN:

- Úsáid HTTPS do URL freastalaí OCSP
- Bailíochtaigh sínithe freagartha OCSP
- Socraigh fad taisce cothrom (úire vs feidhmíocht)
- Úsáid iompar "Fail" i dtimpeallachtaí ardshlándála
- Monatóireacht ar infhaighteacht freastalaí OCSP
- Cuir loighic athiarrachta i bhfeidhm do theipeanna sealadacha
- Logáil gach teip bailíochtaithe OCSP

### ❌ NÁ DÉAN:

- Ná húsáid HTTP d'OCSP i dtáirgeadh
- Ná scipeáil bailíochtú sínithe freagartha OCSP
- Ná cuir freagraí i dtaisce rófhada (> 24 uair)
- Ná déan neamhaird chiúin de theipeanna freastalaí OCSP
- Ná díchumasaigh OCSP i dtáirgeadh gan údar maith

---

## Cur i bhFeidhm Freastalaí OCSP

### Rogha 1: OpenSSL OCSP Responder

```bash
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Rogha 2: BouncyCastle (.NET)

```csharp
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Parse request
        // 2. Check certificate status in database
        // 3. Build response
        // 4. Sign response
        // 5. Return signed response
    }
}
```

### Rogha 3: Seirbhís Tráchtála OCSP

- **DigiCert**: seirbhís bhainistithe OCSP
- **Let's Encrypt**: OCSP saor in aisce dá ndeimhnithe
- **GlobalSign**: réitigh OCSP fiontair

---

## Monatóireacht agus Logáil

### Cumasaigh Logáil Mhionsonraithe

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

### Teachtaireachtaí Loga Samplacha

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Tástáil

### Aonad-Tástálacha

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Tástáil Láimhe

1. **Díchumasaigh OCSP** — deimhnigh go n-oibríonn an feidhmchlár gan OCSP
2. **URL neamhbhailí** — tástáil socruithe `ServerUnavailableBehavior`
3. **Deimhniú bailí** — ba cheart `OcspStatus.Good` a fháil
4. **Freagra taiscthe** — deimhnigh go n-oibríonn cache

---

## Breithnithe Feidhmíochta

### Cumraíocht Taisce

```json
"CacheDurationMinutes": 60
```

**Comhbhabhtálacha:**
- **Gearr (5-15 min)**: sonraí níos úire, ualach OCSP níos airde
- **Fada (60-120 min)**: feidhmíocht níos fearr, riosca sonraí seanda

### Socruithe Teorainn Ama

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Moltaí:**
- Teorainn ama: 10-30 soicind i dtáirgeadh
- Athiarrachtaí: 2-3 iarracht do theipeanna sealadacha

---

## Fabhtcheartú

### Saincheist: Freastalaí OCSP i gcónaí neamh-inrochtana

**Réitigh:**
1. Seiceáil `OcspServerUrl`
2. Deimhnigh go gceadaíonn an balla dóiteáin HTTPS amach
3. Seiceáil go bhfuil freastalaí OCSP ag rith
4. Athbhreithnigh logaí do earráidí timeout

### Saincheist: Gach deimhniú ag teip ar bhailíochtú

**Réitigh:**
1. Deimhnigh go bhfuil sonraí stádais ag freastalaí OCSP
2. Seiceáil slabhra deimhnithe iomlán
3. Cinntigh go bhfuil síniú freagartha OCSP bailí
4. Athbhreithnigh logaí freastalaí OCSP

### Saincheist: Níl cache ag obair

**Réitigh:**
1. Deimhnigh `CacheDurationMinutes > 0`
2. Seiceáil go n-úsáidtear an thumbprint céanna
3. Atosaigh an feidhmchlár chun cache a ghlanadh

---

## Na Chéad Chéimeanna Eile

Chun OCSP a dhéanamh lánfheidhmiúil:

1. ✅ **Cumraíocht Críochnaithe** - tá socruithe réidh
2. ✅ **Comhéadan Seirbhíse Críochnaithe** - API sainmhínithe
3. ✅ **Tástálacha Críochnaithe** - 30+ aonad-tástáil
4. ⏳ **Prótacal OCSP** - RFC 6960 le cur i bhfeidhm
5. ⏳ **Freastalaí OCSP** - freagróir OCSP le himscaradh
6. ⏳ **Comhtháthú** - ceangal le fíordheimhniú mTLS

---

## Tagairtí

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - Sonraíocht OCSP
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Stádas:** ✅ Teimpléad Réidh  
**Prótacal OCSP:** ⏳ Le Cur i bhFeidhm  
**Freastalaí OCSP:** ⏳ Le hImscaradh  
**Tástálacha:** ✅ 30+ Tástáil San Áireamh
