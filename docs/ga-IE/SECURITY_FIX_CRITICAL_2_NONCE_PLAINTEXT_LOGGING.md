# Ceartú Slándála: Luachanna Nonce Logáilte mar Théacs Soiléir (Criticiúil #2)

**Ceartaithe i:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tástálacha:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Cad a bhí mícheart

Bhí dhá shuíomh ag logáil an luach nonce CSP féin go díreach isteach i sruth loga na feidhmchláir:

**`Services/NonceMiddleware.cs` (líne 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (líne 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Cén fáth go bhfuil sé seo criticiúil

Is é nonce CSP an *t-aon* mheicníocht a choisceann instealladh script inlíne nuair atá CSP i bhfeidhm.
Braitheann a shlándáil go hiomlán ar a bheith **rúnda ar feadh shaolré freagartha aonair**.

I dtimpeallacht scamall/gnó, is minic gur féidir le daoine/uirlisí seo logaí feidhmchláir a léamh:
* Foirne oibríochtaí
* Seirbhísí comhiomlánaithe logaí (m.sh. Azure Monitor, Splunk, ELK)
* Cuntas ar bith le cead léitheora ar an stór logaí

Is féidir le haon duine a fheiceann líne loga ina bhfuil `Nonce: <value>` clib `<script>` inlíne leis an nonce sin
a instealladh agus an brabhsálaí a chur á fhorghníomhú, rud a sheachnaíonn CSP go hiomlán.
Fiú má rothlaíonn an nonce in aghaidh an iarratais, is féidir le hionsaitheoir le rochtain bheo ar logaí gníomhú
laistigh den fhuinneog iarratais chéanna.

---

## Cad a ceartaíodh

Cuireadh teachtaireachtaí in ionad an dá ráiteas logála a dhearbhaíonn *stádas* giniúna nonce
gan an luach féin a nochtadh:

**`NonceMiddleware.cs`:**
```csharp
// ROIMH (leochaileach):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// TAR ÉIS (sábháilte):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// ROIMH (leochaileach):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// TAR ÉIS (sábháilte):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Conas é seo a choinneáil ceartaithe

1. **Ná logáil luach an nonce riamh.** Is féidir leis na logaí dearbhú gur gineadh nó gur aisghabhadh nonce
   (stádas ratha/teipe), ach níor cheart an tsreang nonce féin a bheith in aon pharaiméadar logála,
   réimse logála struchtúrtha, ná idirsreangú téacs.

2. **Déan athbhreithniú ar aon ráiteas logála nua i gcód a bhaineann le nonce** (`NonceMiddleware`,
   `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) chun a chinntiú nach bhfuil
   luach an nonce san áireamh.

3. **Ná nocht an nonce i dteiliméadracht, meadrachtaí ná rianuithe dáileáilte** ar an gcúis chéanna.
   Is minic a sheoltar tréithe rianaithe agus clibeanna span chuig cúlchríocha comhiomlánaithe logaí.

4. **Caithfear an nonce a mheas mar rún in aghaidh an iarratais.** Is féidir é a stóráil in `HttpContext.Items`
   le húsáid laistigh den phíblíne rindreála iarratais aonair, ach níor cheart dó an próiseas a fhágáil
   trí chainéal inbhraite ar bith seachas ceanntásc freagartha HTTP agus an tréith `nonce="..."` san HTML
   a chosnaíonn sé.

### Tástálacha a fhorfheidhmíonn an ceartú seo

| Tástáil | Cad a aimsíonn sí |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Teipeann má chuirtear an tsreang nonce ar ais in aon teachtaireacht logála i `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Teipeann má chuirtear an tsreang nonce ar ais in aon teachtaireacht logála i `NonceMiddleware` |
