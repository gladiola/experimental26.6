# Ceartú Slándála: Nonceanna Cúltaca Crua-Chódaithe (Criticiúil #3)

**Ceartaithe i:** `Services/OptimizedNonceMiddleware.cs`  
**Tástálacha:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Cad a bhí mícheart

Bhí trí shreang liteartha crua-chódaithe in `OptimizedNonceMiddleware` a úsáideadh mar luachanna nonce cúltaca
nuair a theip ar ghnáthghiniúint nonce nó nuair nár ritheadh í fós:

| Suíomh | Luach crua-chódaithe |
|----------|-----------------|
| `InvokeAsync` — an chéad iarratas, catalóg folamh | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — d'fhill giniúint sreang fholamh | `"fallback-nonce"` |
| `InvokeAsync` — conair eisceachta | `"error-fallback-nonce"` |

### Cén fáth go bhfuil sé seo criticiúil

**Ní bhíonn nonce slán ach mura féidir le hionsaitheoir é a thuar.** Tá litearthaí crua-chódaithe
sa chód foinse agus mar sin ar eolas ag aon duine le rochtain ar an stór (lena n-áirítear ionsaitheoir
ar bhain rochtain ar an bhfoinse amach nó a dhíchóimeáil an dénártha).

Tagann an baol sonrach as go ngníomhachtaítear na cosáin cúltaca seo faoi **choinníollacha earráide** —
go díreach na cásanna is dóichí a dhéanfadh ionsaitheoir a innealtóireacht (m.sh. Key Vault a dhéanamh
neamh-inrochtana sealadach trí theorannú ráta nó cur isteach líonra). Nuair a ísliúitear an feidhmchlár
go nonce intuartha, éiríonn ceanntásc CSP maisiúil: ní dhéanann an t-ionsaitheoir ach
`<script nonce="fallback-nonce">` a instealladh agus forghníomhaíonn an brabhsálaí é.

### Cód bunchúise (roimh an gceartú)

```csharp
// Céad iarratas roimh ghiniúint ar bith
existingNonce = "bootstrap-nonce-placeholder";

// D'fhill giniúint nonce folamh
nonce = "fallback-nonce";

// Conair eisceachta
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Cad a ceartaíodh

Glaonn gach ceann de na trí chonair cúltaca anois ar `Nonce.GenerateSecureNonce()` chun nonce úr,
dothuartha 16-bheart a tháirgeadh ag am rite:

```csharp
// ROIMH (leochaileach):
existingNonce = "bootstrap-nonce-placeholder";
// TAR ÉIS (sábháilte):
existingNonce = Nonce.GenerateSecureNonce();

// ROIMH (leochaileach):
nonce = "fallback-nonce";
// TAR ÉIS (sábháilte):
nonce = Nonce.GenerateSecureNonce();

// ROIMH (leochaileach):
context.Items["Nonce"] = "error-fallback-nonce";
// TAR ÉIS (sábháilte):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

Úsáideann `Nonce.GenerateSecureNonce()` `RandomNumberGenerator.Fill` (CSPRNG) chun 16 bheart randamacha
cripteagrafacha a ghiniúint ionchódaithe mar Base64. Toisc gur modh statach é gan spleáchas Key Vault,
tá sé sábháilte le glaoch fiú nuair atá Key Vault as feidhm — go díreach an coinníoll earráide a nocht
an cúltaca crua-chódaithe roimhe seo.

---

## Conas é seo a choinneáil ceartaithe

1. **Ná tabhair isteach litearthach nonce crua-chódaithe riamh** áit ar bith sa chód, beag beann ar an gcomhthéacs
   (cúltaca, tástáil, áitsealbhóir, sampla tráchta a chóipeáiltear, srl.).

2. **Caithfidh gach cosán cóid a shocraíonn `context.Items["Nonce"]` luach randamach cripteagrafach a úsáid.**
   Glaoigh ar `Nonce.GenerateSecureNonce()` nó `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Ná taisce nonce aonair thar iarratais.** Caithfidh gach iarratas nonce úr dá chuid féin a fháil.

4. **Is iad cosáin earráide na cinn is contúirtí.** Má theipeann ar ghiniúint nonce ar chúis ar bith,
   caithfidh an freagra nonce randamach a fháil fós, ní cúltaca intuartha.

5. **Déan athbhreithniú ar athruithe amach anseo ar `OptimizedNonceMiddleware`** — go háirithe na trí bhrainse
   ina socraítear nonce: an bhrainse ignore-path, an bhrainse giniúna folaimh, agus an bhrainse láimhseála eisceachta.

### Tástálacha a fhorfheidhmíonn an ceartú seo

| Tástáil | Cad a aimsíonn sí |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Teipeann má chuirtear `"bootstrap-nonce-placeholder"` ar ais sa bhrainse chéad iarratais |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Teipeann má chuirtear `"fallback-nonce"` ar ais sa bhrainse giniúna folaimh |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Teipeann má chuirtear `"error-fallback-nonce"` ar ais sa láimhseálaí eisceachta |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Teipeann má tháirgeann aon chúltaca an nonce céanna faoi dhó i 50 glao as a chéile (rud a dhéanfadh aon shreang crua-chódaithe) |
