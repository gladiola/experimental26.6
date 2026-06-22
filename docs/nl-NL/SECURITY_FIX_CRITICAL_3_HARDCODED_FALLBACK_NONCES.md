# Securityfix: Hardcoded fallback-nonces (Kritiek #3)

**Gefixt in:** `Services/OptimizedNonceMiddleware.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Wat was er mis

`OptimizedNonceMiddleware` bevatte drie hardcoded stringliterals die als fallback-nonce werden gebruikt wanneer normale nonce-generatie faalde of nog niet had gedraaid:

| Locatie | Hardcoded waarde |
|----------|-----------------|
| `InvokeAsync` — eerste request, catalogus leeg | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — generatie gaf lege string terug | `"fallback-nonce"` |
| `InvokeAsync` — exceptionpad | `"error-fallback-nonce"` |

### Waarom dit kritiek is

**Een nonce is alleen veilig als een aanvaller die niet kan voorspellen.** Hardcoded waarden staan in source control en zijn dus bekend voor iedereen met repositorytoegang (inclusief aanvallers met broncode- of binarytoegang).

Het specifieke risico is dat deze fallbackpaden juist bij **foutsituaties** worden geactiveerd — precies de situaties die een aanvaller probeert op te wekken (bijv. Key Vault tijdelijk onbeschikbaar maken via rate-limiting of netwerkverstoring). Als de app dan degradeert naar een voorspelbare nonce, wordt de CSP-header decoratief: de aanvaller injecteert simpelweg `<script nonce="fallback-nonce">` en de browser voert dit uit.

### Root-cause code (vóór fix)

```csharp
// Eerste request voordat nonce is gegenereerd
existingNonce = "bootstrap-nonce-placeholder";

// Nonce-generatie gaf leeg resultaat
nonce = "fallback-nonce";

// Exceptionpad
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Wat is gefixt

Alle drie fallbackpaden roepen nu `Nonce.GenerateSecureNonce()` aan om een verse, onvoorspelbare 16-byte random nonce op runtime te genereren:

```csharp
// BEFORE (kwetsbaar):
existingNonce = "bootstrap-nonce-placeholder";
// AFTER (veilig):
existingNonce = Nonce.GenerateSecureNonce();

// BEFORE (kwetsbaar):
nonce = "fallback-nonce";
// AFTER (veilig):
nonce = Nonce.GenerateSecureNonce();

// BEFORE (kwetsbaar):
context.Items["Nonce"] = "error-fallback-nonce";
// AFTER (veilig):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` gebruikt `RandomNumberGenerator.Fill` (CSPRNG) om 16 cryptografisch willekeurige bytes te genereren, gecodeerd als Base64. Omdat het een statische methode is zonder Key Vault-afhankelijkheid, is dit veilig aan te roepen wanneer Key Vault onbeschikbaar is — precies de foutconditie die eerder de hardcoded fallback blootlegde.

---

## Hoe je dit gefixt houdt

1. **Voeg nooit een hardcoded nonce-literal toe** in de codebase, ongeacht context (fallback, test, placeholder, of commentaarvoorbeeld dat gekopieerd kan worden).

2. **Elk codepad dat `context.Items["Nonce"]` zet moet cryptografisch willekeurig zijn.** Gebruik `Nonce.GenerateSecureNonce()` of `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Cache nooit één nonce over requests heen.** Elk request moet een verse nonce krijgen.

4. **Errorpaden zijn het gevaarlijkst.** Als nonce-generatie faalt, moet de response nog steeds een random nonce krijgen, nooit een voorspelbare fallback.

5. **Review toekomstige wijzigingen in `OptimizedNonceMiddleware`** met name de drie branches waar de nonce gezet wordt: ignore-path, lege generatie, en exception-handler.

### Tests die deze fix afdwingen

| Test | Wat het opvangt |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Faal als `"bootstrap-nonce-placeholder"` terugkomt in de eerste-request-branch |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Faal als `"fallback-nonce"` terugkomt in de lege-generatie-branch |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Faal als `"error-fallback-nonce"` terugkomt in de exception-handler |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Faal als fallback in 50 opeenvolgende aanroepen twee keer dezelfde nonce produceert (zoals bij hardcoded strings) |
