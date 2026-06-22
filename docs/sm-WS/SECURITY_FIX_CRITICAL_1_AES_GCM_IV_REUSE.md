# Faaleleiga Saogalemu: Toe Faaaogaina AES-GCM IV i Nonce Generation (Mataʻutia #1)

**Faaleleia i:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`

---

## Mea na sese

Sa faʻaaogā e le vasega `Nonce` le AES-GCM ma se IV tumau e toe faaaoga i valaau taitasi. I AES-GCM, o le toe faaaogaina o le IV i le ki tutusa o se sese cryptography mataʻutia:

- e mafai ona fesoasoani i le toe maua o sootaga i plaintext,
- e mafai ona taitai i tag forgery,
- e solia ai le integrity guarantees.

Mo CSP nonce, e le manaʻomia encryption. E naʻo:

1. **le mafai ona valoia**,
2. **ese i request taʻitasi**.

---

## Mea na faaleleia

`Nonce.GenerateSecureNonce()` ua gaosia saʻo 16 random bytes e ala i `RandomNumberGenerator.Fill` ma toe faafoʻi Base64 string.

Iʻuga:

- leai se IV mai Key Vault,
- leai se AES-GCM i nonce generation,
- faigofie ma saogalemu atili.

Faʻaopoopo, na faaleleia `NonceCatalogService.GetANonce` i le retrieval atomic e ala i `TryGetValue(out ...)` ina ia aveese race pattern check-then-lookup.

---

## Faʻafefea ona tumau lenei faaleleiga

1. Aua le toe faaofi IV/key dependency mo nonce generation.
2. Aua le suia i scheme e toe faaaoga IV/counter.
3. Taofi nonce i le itiiti ifo 16 bytes.
4. Aua le suia CSPRNG i `Random()` masani.
5. Taofi retrieval pattern thread-safe i catalog service.

---

## Suʻega e puipui ai lenei faaleleiga

- suʻega constructor/signature,
- suʻega Base64 validity,
- suʻega 16-byte minimum,
- suʻega uniqueness i successive generations,
- suʻega entropy,
- suʻega concurrency mo nonce catalog.
