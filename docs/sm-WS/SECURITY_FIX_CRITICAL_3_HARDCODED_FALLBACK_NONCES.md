# Faaleleiga Saogalemu: Hardcoded Fallback Nonces (Mataʻutia #3)

**Faaleleia i:** `Services/OptimizedNonceMiddleware.cs`

---

## Mea na sese

Sa i ai fallback nonce literals tumau i code pe a toilalo nonce generation:

- `bootstrap-nonce-placeholder`
- `fallback-nonce`
- `error-fallback-nonce`

Nonce tumau e mafai ona valoia ma faʻaogaina e bypass CSP pe a tupu se error path.

---

## Mea na faaleleia

Ua sui fallback paths uma e valaau `Nonce.GenerateSecureNonce()` ina ia gaosia nonce fou, random, ma lē valoia i taimi uma.

O lenei e saogalemu foi i tulaga e paʻū Key Vault, aua e le manaʻomia ai le Key Vault mo fallback nonce generation.

---

## Faʻafefea ona tumau lenei faaleleiga

1. Aua le toe faʻaofi hardcoded nonce i soʻo se branch.
2. Branch uma e seti ai `context.Items["Nonce"]` e tatau ona random.
3. Aua le toe faaaoga nonce e tasi i requests eseese.
4. Error paths e tatau ona tumau cryptographically random.

---

## Suʻega e puipui ai lenei faaleleiga

- suʻega mo branch catalog-empty,
- suʻega mo empty-generation branch,
- suʻega mo exception branch,
- suʻega uniqueness i fallback calls faasolosolo.
