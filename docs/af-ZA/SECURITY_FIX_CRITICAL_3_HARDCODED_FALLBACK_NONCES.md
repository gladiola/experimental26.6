# Sekuriteitsregstelling: Hardgekodeerde Terugval-Nonces (Kritiek #3)

**Reggestel in:** `Services/OptimizedNonceMiddleware.cs`  
**Toetse:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Wat Was Fout

`OptimizedNonceMiddleware` het drie hardgekodeerde stringleterale bevat wat as terugval-nonce-waardes gebruik is wanneer normale nonce-generering misluk het of nog nie uitgevoer is nie:

| Ligging | Hardgekodeerde Waarde |
|---------|----------------------|
| `InvokeAsync` — eerste versoek, katalogus leeg | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — generering het leë string teruggegee | `"fallback-nonce"` |
| `InvokeAsync` — uitsonderingspad | `"error-fallback-nonce"` |

### Waarom Dit Kritiek Is

**'n Nonce is slegs veilig as 'n aanvaller dit nie kan voorspel nie.** Hardgekodeerde literale word na bronbeheer gepleeg en is dus bekend aan enigeen met opslagplaastoegang (insluitend enige aanvaller wat bronkodestoegang verkry of die binêre lêer gedekompileer het).

Die spesifieke gevaar is dat hierdie terugvalpaaie geaktiveer word deur **fouttoestande** — presies die situasies wat 'n aanvaller die grootste kans het om te bewerk (bv. Key Vault tydelik onbeskikbaar maak via koersbeperking of netwerksteurnis). Wanneer die toepassing grasieus na 'n voorspelbare nonce afgraad, word die CSP-opskrif dekoratief: die aanvaller spuit eenvoudig `<script nonce="fallback-nonce">` in en die blaaier voer dit uit.

### Oorsprong-kode (voor regstelling)

```csharp
// Eerste versoek voor enige nonce gegenereer
existingNonce = "bootstrap-nonce-placeholder";

// Nonce-generering het leeg teruggekeer
nonce = "fallback-nonce";

// Uitsonderingspad
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Wat Reggestel Is

Al drie terugvalpaaie roep nou `Nonce.GenerateSecureNonce()` aan om 'n vars, onvoorspelbare 16-greep ewekansige nonce tydens uitvoering te produseer:

```csharp
// VOOR (kwesbaar):
existingNonce = "bootstrap-nonce-placeholder";
// NA (veilig):
existingNonce = Nonce.GenerateSecureNonce();

// VOOR (kwesbaar):
nonce = "fallback-nonce";
// NA (veilig):
nonce = Nonce.GenerateSecureNonce();

// VOOR (kwesbaar):
context.Items["Nonce"] = "error-fallback-nonce";
// NA (veilig):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` gebruik `RandomNumberGenerator.Fill` ('n CSPRNG) om 16 kriptografies ewekansige grepe as Base64 te genereer. Omdat dit 'n statiese metode is sonder Key Vault-afhanklikheid, is dit veilig om te roep selfs wanneer Key Vault onbeskikbaar is — presies die foutsituasie wat voorheen die hardgekodeerde terugval blootgestel het.

---

## Hoe om Dit Reggestel te Hou

1. **Stel nooit 'n hardgekodeerde nonce-literaal** iewers in die kodebaseer in nie, ongeag die konteks (terugval, toets, plekhouer, kommentaarvoorbeeld wat gekopieer word, ens.).

2. **Elke kodepad wat `context.Items["Nonce"]` stel, moet 'n kriptografies ewekansige waarde gebruik.** Roep `Nonce.GenerateSecureNonce()` of `RandomNumberGenerator.GetBytes(16)` + Base64 aan.

3. **Cache nie 'n enkele nonce oor versoeke nie.** Elke versoek moet sy eie vars nonce ontvang.

4. **Foutpaaie is die gevaarlikste.** As nonce-generering om enige rede misluk, moet die respons steeds 'n ewekansige nonce ontvang, nooit 'n voorspelbare terugval nie.

5. **Hersien enige toekomstige veranderinge aan `OptimizedNonceMiddleware`** — veral die drie takke waar die nonce gestel kan word: die ignoreer-pad-tak, die leë-generering-tak en die uitsonderingsverwerker-tak.

### Toetse wat Hierdie Regstelling Afdwing

| Toets | Wat Dit Vang |
|-------|-------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Misluk as `"bootstrap-nonce-placeholder"` herbekendgestel word in die eerste-versoek-tak |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Misluk as `"fallback-nonce"` herbekendgestel word in die leë-generering-tak |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Misluk as `"error-fallback-nonce"` herbekendgestel word in die uitsonderingsverwerker |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Misluk as enige terugval dieselfde nonce twee keer in 50 agtereenvolgende aanroepe produseer (wat enige hardgekodeerde string sou doen) |
