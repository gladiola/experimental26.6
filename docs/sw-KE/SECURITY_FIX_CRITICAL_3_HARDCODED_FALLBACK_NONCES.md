# Marekebisho ya Usalama: Hardcoded Fallback Nonces (Critical #3)

**Imerekebishwa katika:** `Services/OptimizedNonceMiddleware.cs`  
**Majaribio:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Nini Kilikuwa Kibaya

`OptimizedNonceMiddleware` ilikuwa na string literals tatu za hardcoded zilizotumika kama fallback
nonce values wakati uzalishaji wa kawaida wa nonce uliposhindwa au ulikuwa bado haujaendeshwa:

| Mahali | Thamani ya Hardcoded |
|----------|-----------------|
| `InvokeAsync` — request ya kwanza, catalog tupu | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — generation ilirudisha string tupu | `"fallback-nonce"` |
| `InvokeAsync` — exception path | `"error-fallback-nonce"` |

### Kwa Nini Hili ni Critical

**Nonce ni salama tu ikiwa mshambuliaji hawezi kuitabiri.** Hardcoded literals huwekwa kwenye
source control na kwa hiyo hujulikana kwa yeyote mwenye ufikiaji wa repository (akiwemo mshambuliaji yeyote
aliyepata ufikiaji wa source au aliyedecompile binary).

Hatari mahsusi ni kwamba fallback paths hizi huwashwa na **hali za makosa** — hasa
hali ambazo mshambuliaji ana uwezekano mkubwa wa kujaribu kusababisha (kwa mfano, kufanya Key Vault isipatikane kwa muda kwa kutumia rate-limiting au network disruption). Wakati programu inaposhuka kwa utulivu hadi nonce inayotabirika, kichwa cha CSP kinakuwa cha mapambo tu: mshambuliaji anaingiza
`<script nonce="fallback-nonce">` na browser huitekeleza.

### Msimbo wa Chanzo cha Tatizo (kabla ya marekebisho)

```csharp
// First request before any nonce generated
existingNonce = "bootstrap-nonce-placeholder";

// Nonce generation returned empty
nonce = "fallback-nonce";

// Exception path
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Nini Kilirekebishwa

Fallback paths zote tatu sasa hupiga `Nonce.GenerateSecureNonce()` ili kuzalisha nonce mpya,
isiyotabirika ya bytes 16 ya random wakati wa runtime:

```csharp
// BEFORE (vulnerable):
existingNonce = "bootstrap-nonce-placeholder";
// AFTER (safe):
existingNonce = Nonce.GenerateSecureNonce();

// BEFORE (vulnerable):
nonce = "fallback-nonce";
// AFTER (safe):
nonce = Nonce.GenerateSecureNonce();

// BEFORE (vulnerable):
context.Items["Nonce"] = "error-fallback-nonce";
// AFTER (safe):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` hutumia `RandomNumberGenerator.Fill` (CSPRNG) kuzalisha bytes 16
za cryptographically random zilizo na Base64 encoding. Kwa sababu ni static method lisilo na
utegemezi wa Key Vault ni salama kupigwa hata Key Vault isipokuwepo — hali ileile ya kosa
iliyokuwa awali ikifichua hardcoded fallback.

---

## Jinsi ya Kuhakikisha Hili Linabaki Limeboreshwa

1. **Usiwahi kuingiza hardcoded nonce literal** mahali popote katika codebase, bila kujali
   muktadha (fallback, test, placeholder, mfano wa maoni unaonakiliwa, n.k.).

2. **Kila code path inayoweka `context.Items["Nonce"]` lazima itumie thamani ya cryptographically random.**
   Piga `Nonce.GenerateSecureNonce()` au `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Usihifadhi nonce moja kwenye cache kwa requests mbalimbali.** Kila request lazima ipokee nonce yake mpya.

4. **Error paths ndizo hatari zaidi.** Ikiwa uzalishaji wa nonce unashindwa kwa sababu yoyote, response
   bado inapaswa kupokea nonce ya random, si fallback inayotabirika.

5. **Kagua mabadiliko yoyote yajayo katika `OptimizedNonceMiddleware`** — hasa matawi matatu
   ambapo nonce inaweza kuwekwa: tawi la ignore-path, tawi la empty-generation, na tawi la
   exception-handler.

### Majaribio Yanayolazimisha Marekebisho Haya

| Jaribio | Kinachogundua |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Hushindwa ikiwa `"bootstrap-nonce-placeholder"` itarudishwa katika tawi la request ya kwanza |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Hushindwa ikiwa `"fallback-nonce"` itarudishwa katika tawi la empty-generation |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Hushindwa ikiwa `"error-fallback-nonce"` itarudishwa katika exception handler |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Hushindwa ikiwa fallback yoyote itazalisha nonce ileile mara mbili katika miito 50 mfululizo (jambo ambalo string yoyote ya hardcoded ingefanya) |

