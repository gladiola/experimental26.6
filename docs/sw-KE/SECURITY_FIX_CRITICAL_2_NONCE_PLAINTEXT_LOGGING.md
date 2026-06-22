# Marekebisho ya Usalama: Thamani za Nonce Kuandikwa Kwenye Kumbukumbu kama Plaintext (Critical #2)

**Imerekebishwa katika:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Majaribio:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Nini Kilikuwa Kibaya

Maeneo mawili yaliandika thamani halisi ya nonce ya CSP bila kuficha ndani ya stream ya kumbukumbu za programu:

**`Services/NonceMiddleware.cs` (mstari wa 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (mstari wa 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Kwa Nini Hili ni Critical

Nonce ya CSP ndiyo *utaratibu pekee* unaozuia inline-script injection mara tu CSP inapotekelezwa. Usalama
wake unategemea kabisa kubaki **siri kwa muda wa response moja**.

Kumbukumbu za programu katika mazingira ya cloud/enterprise kwa kawaida zinaweza kusomwa na:
* Timu za operations
* Huduma za kujumlisha kumbukumbu (kwa mfano, Azure Monitor, Splunk, ELK)
* Akaunti yoyote yenye reader access kwenye log sink

Mtu yeyote anayeweza kusoma mstari wa kumbukumbu wenye `Nonce: <value>` anaweza kuingiza tag ya inline `<script>`
iliyo na thamani hiyo ya nonce na kufanya browser iteekeleze, hivyo kupita CSP kabisa. Hata kama
nonce inabadilika kwa kila request, mshambuliaji mwenye ufikiaji wa moja kwa moja wa kumbukumbu anaweza kuchukua hatua
ndani ya dirisha la request hiyo hiyo.

---

## Nini Kilirekebishwa

Kauli zote mbili za kumbukumbu zilihamishwa kwa ujumbe unaothibitisha *hali* ya uzalishaji wa nonce
bila kufichua thamani:

**`NonceMiddleware.cs`:**
```csharp
// BEFORE (vulnerable):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// AFTER (safe):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// BEFORE (vulnerable):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// AFTER (safe):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Jinsi ya Kuhakikisha Hili Linabaki Limeboreshwa

1. **Usiwahi kuandika thamani ya nonce kwenye kumbukumbu.** Ujumbe wa kumbukumbu unaweza kuthibitisha kuwa nonce ilizalishwa au
   ilirejeshwa (hali ya success/failure), lakini string ya nonce yenyewe haipaswi kamwe kuonekana katika parameter yoyote ya
   kumbukumbu, structured-logging field, au string interpolation.

2. **Kagua kauli mpya yoyote ya kumbukumbu katika msimbo unaohusiana na nonce** (`NonceMiddleware`,
   `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) ili kuhakikisha thamani ya
   nonce haijajumuishwa.

3. **Usifichue nonce katika telemetry, metrics, au distributed traces** kwa sababu hizo hizo.
   Trace attributes na span tags mara nyingi hutumwa kwa backends za kujumlisha kumbukumbu.

4. **Nonce lazima ichukuliwe kama siri ya kila request.** Inaweza kuhifadhiwa katika `HttpContext.Items`
   kwa matumizi ndani ya rendering pipeline ya request moja, lakini haipaswi kutoka nje ya process kupitia
   channel yoyote inayoonekana isipokuwa HTTP response header na attribute ya `nonce="..."` katika
   HTML inayolindwa nayo.

### Majaribio Yanayolazimisha Marekebisho Haya

| Jaribio | Kinachogundua |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Hushindwa ikiwa string ya nonce itarudishwa katika ujumbe wowote wa kumbukumbu ndani ya `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Hushindwa ikiwa string ya nonce itarudishwa katika ujumbe wowote wa kumbukumbu ndani ya `NonceMiddleware` |

