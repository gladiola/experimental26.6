# Faaleleiga Saogalemu: Nonce sa Tusitusia Plaintext i Logs (Mataʻutia #2)

**Faaleleia i:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

---

## Mea na sese

Sa i ai nofoaga e lua na tusia ai le nonce moni i logs. O le CSP nonce e tatau ona tumau lilo i le olaga o le response e tasi.

A tusia i logs, soo se tagata e iai log access e mafai ona:

- faitau le nonce,
- faaaoga i inline script injection,
- bypass CSP.

---

## Mea na faaleleia

Na suia log statements ina ia tusia **status messages** naʻo:

- “Nonce retrieved for request.”
- “Nonce generated successfully.”

E leai se nonce value moni i logs.

---

## Faʻafefea ona tumau lenei faaleleiga

1. Aua lava neʻi log-inā le nonce value.
2. Iloilo log fou i nonce-related services.
3. Aua le tuʻu nonce i telemetry/spans/metrics fields.
4. Taofi nonce i `HttpContext.Items` mo request totonu naʻo.

---

## Suʻega e puipui ai lenei faaleleiga

- suʻega e siaki e le toe aliali nonce string i log output mo refresher service,
- suʻega e siaki e le toe aliali nonce string i middleware logs.
