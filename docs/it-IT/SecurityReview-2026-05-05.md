# Revisione Sicurezza — WebAppExperimental266

**Data:** 2026-05-05  
**Ambito:** Analisi statica dell'intero codice

---

## Tabella riepilogativa

| # | Area | Severità |
|---|------|----------|
| 1 | Riutilizzo IV AES-GCM nella generazione nonce | Critica (risolta) |
| 2 | Nonce loggato in chiaro | Critica (risolta) |
| 3 | Stringhe nonce fallback hardcoded | Critica (risolta) |
| 4 | Dizionario nonce globale non thread-safe | Alta (risolta) |
| 5 | Validazione issuer mTLS commentata | Alta (risolta) |
| 6 | Revoca certificati mTLS disattivata di default | Alta (risolta) |
| 7 | OCSP sempre valido (stub) | Alta (risolta) |
| 8 | Auth/authz disattivate di default | Alta (risolta) |
| 9 | Security headers troppo tardi nella pipeline | Alta (risolta) |
| 10 | Cookie sessione senza Secure + SameSite | Media (risolta) |
| 11 | Header globale Set-Cookie malformato | Media (risolta) |
| 12 | Content-Type forzato a text/html ovunque | Media (risolta) |
| 13 | AllowedHosts wildcard | Media (risolta) |
| 14 | Nonce non applicato agli script nel layout | Media (risolta) |
| 15 | Header Referrer-Policy mancante | Media (risolta) |
| 16 | PII loggati in chiaro | Bassa (risolta) |
| 17 | Porzione di connection string nei log | Bassa (risolta) |
| 18 | Operazioni Key Vault in modalità stub | Bassa (risolta) |
| 19 | Header X-XSS-Protection deprecato | Bassa (risolta) |

---

## Dettagli principali

### 1) Riutilizzo IV AES-GCM (Critica) — risolto

La generazione nonce usava AES-GCM con IV fisso, condizione crittograficamente insicura. È stata sostituita da generazione nonce random con CSPRNG (`RandomNumberGenerator`) e codifica Base64.

### 2) Nonce nei log (Critica) — risolto

Il valore nonce era scritto nei log applicativi. Ora i log riportano solo stato/esito e non il valore segreto.

### 3) Nonce fallback hardcoded (Critica) — risolto

Valori prevedibili hardcoded sono stati sostituiti con nonce random generati runtime.

### 4) Dizionario nonce non thread-safe (Alta) — risolto

Sostituito con struttura concorrente e pattern di accesso atomico.

### 5) Validazione issuer mTLS (Alta) — risolto

Codice di validazione issuer ripristinato/attivato.

### 6) Revoca certificati mTLS (Alta) — risolto

Default e configurazione rivisti per uso sicuro in produzione.

### 7) OCSP stub (Alta) — risolto

Comportamento template reso esplicito e aggiornato il flusso di sicurezza associato.

### 8) Auth/Authz disabilitate di default (Alta) — risolto

Configurazione di base aggiornata per evitare deploy accidentalmente aperti.

### 9) Security headers tardivi (Alta) — risolto

Ordine middleware corretto per coprire anche risposte che terminano presto.

### 10–15) Problemi medi — risolti

Correzioni su cookie/sessione, header malformati, content-type, host validation, nonce nei tag script e referrer policy.

### 16–19) Problemi bassi/informativi — risolti

Migliorie su logging PII, logging segreti parziali, integrazione Key Vault e header deprecati.

---

## Conclusione

La revisione ha individuato vulnerabilità critiche e ad alta severità nelle aree nonce/CSP, mTLS e pipeline di sicurezza HTTP. Le correzioni riportate nei commit indicati nel documento sorgente inglese risultano applicate e coperte da test automatici specifici.
