# Revisione della Sicurezza — WebAppExperimental266

**Data:** 2026-05-06
**Ambito:** Analisi statica completa del codice sorgente (seguito alla revisione del 2026-05-05)
**Revisore:** Revisione della Sicurezza Automatizzata

---

## Sommario Esecutivo

Questa revisione di follow-up conferma che tutte le 19 vulnerabilità identificate nella revisione della sicurezza del 2026-05-05 sono state risolte. La revisione identifica anche 5 nuovi risultati o residui scoperti durante questa sessione. La postura di sicurezza generale dell'applicazione è migliorata significativamente rispetto alla revisione precedente.

---

## Stato dei Risultati Precedenti (2026-05-05)

Tutti i 19 risultati precedenti sono **confermati come risolti**:

| # | Risultato | Gravità | Stato |
|---|-----------|---------|-------|
| 1 | Riutilizzo IV AES-GCM nella generazione del nonce | 🔴 Critico | ✅ Risolto |
| 2 | Nonce registrato in testo in chiaro | 🔴 Critico | ✅ Risolto |
| 3 | Stringhe nonce di fallback codificate | 🔴 Critico | ✅ Risolto |
| 4 | Dizionario nonce globale non thread-safe | 🟠 Alto | ✅ Risolto |
| 5 | Validazione dell'emittente mTLS commentata | 🟠 Alto | ✅ Risolto |
| 6 | Controllo di revoca mTLS disabilitato per impostazione predefinita | 🟠 Alto | ✅ Risolto |
| 7 | OCSP restituisce sempre valido (stub) | 🟠 Alto | ✅ Risolto |
| 8 | Autenticazione/autorizzazione disabilitata per impostazione predefinita nella configurazione | 🟠 Alto | ✅ Risolto |
| 9 | Header di sicurezza applicati troppo tardi nel pipeline | 🟠 Alto | ✅ Risolto |
| 10 | Cookie di sessione mancante di `Secure` + `SameSite` | 🟡 Medio | ✅ Risolto |
| 11 | Header `Set-Cookie` globale malformato | 🟡 Medio | ✅ Risolto |
| 12 | `Content-Type` forzato a `text/html` ovunque | 🟡 Medio | ✅ Risolto |
| 13 | `AllowedHosts` impostato su wildcard | 🟡 Medio | ✅ Risolto |
| 14 | Nonce non applicato ai tag `<script>` nel layout | 🟡 Medio | ✅ Risolto |
| 15 | Header `Referrer-Policy` mancante | 🟡 Medio | ✅ Risolto |
| 16 | PII registrata in testo in chiaro | 🔵 Basso | ✅ Risolto |
| 17 | Stringa di connessione parziale nei log | 🔵 Basso | ✅ Risolto |
| 18 | Le operazioni Key Vault sono stub | 🔵 Basso | ✅ Risolto |
| 19 | `X-XSS-Protection: 1; mode=block` obsoleto | 🔵 Basso | ✅ Risolto |

---

## Nuovi Risultati / Residui

| # | Area | Gravità |
|---|------|---------|
| 20 | NonceRefresherService mantiene dipendenze del costruttore Key Vault inutilizzate | 🟠 Alto |
| 21 | La cache interna di OcspValidationService usa un Dictionary non thread-safe | 🟡 Medio |
| 22 | Lo stub di validazione OCSP è ancora presente — fallisce chiuso ma non implementato | 🔵 Basso |
| 23 | mTLS con AllowedIssuers vuoto rifiuta tutti i certificati (fail-closed, non documentato) | 🔵 Basso |
| 24 | OcspSettings.ServerUnavailableBehavior predefinito a "Warn" (consente il passaggio in caso di errore) | 🔵 Basso |

---

## Risultati Dettagliati

### ✅ Correzioni Confermate dal 2026-05-05

#### 1. Riutilizzo IV AES-GCM — Risolto

**File:** `Models/Main_Objects/Nonce.cs`

La generazione del nonce basata su AES-GCM è stata completamente sostituita. `Nonce.GenerateSecureNonce()` ora chiama `RandomNumberGenerator.Fill(randomBytes)` su 16 byte casuali e restituisce una stringa Base64. Nessuna dipendenza da Key Vault, nessun IV, nessuna crittografia — esattamente l'approccio corretto per un nonce CSP.

---

#### 2. I Valori Nonce Non Vengono Più Registrati — Risolto

**File:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Entrambi i file registrano ora solo messaggi di stato (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) e mai il valore del nonce stesso.

---

#### 3. Nonce di Fallback Codificati Rimossi — Risolto

**File:** `Services/OptimizedNonceMiddleware.cs`

Tutte e tre le stringhe letterali codificate (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) sono state sostituite con chiamate a `Nonce.GenerateSecureNonce()` sia nei percorsi normali che in quelli di fallback delle eccezioni.

---

#### 4. Dizionario Nonce Thread-Safe — Risolto

**File:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` è stato sostituito con `ConcurrentDictionary<string, Nonce>`. `GetANonce` ora utilizza una singola chiamata atomica `TryGetValue` invece di una verifica in due fasi.

---

#### 5. Validazione dell'Emittente mTLS Ora Funzionale — Risolto

**File:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Il blocco di validazione dell'emittente commentato è stato sostituito da una chiamata a `mtlsSettings.IsIssuerAllowed(issuer)`, che esegue una corrispondenza di sottostringa senza distinzione tra maiuscole e minuscole rispetto a `AllowedIssuers`. Quando la lista è vuota (non configurata), il metodo restituisce `false`, rifiutando tutti i certificati (fail-closed).

---

#### 6. Il Controllo di Revoca mTLS È Abilitato per Impostazione Predefinita — Risolto

**File:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` ora ha come valore predefinito `true`. Il file `appsettings.template.json` specifica anche `"CheckCertificateRevocation": true`.

---

#### 7. Lo Stub OCSP Ora Fallisce Chiuso — Risolto

**File:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` ora restituisce `IsValid = false` con `OcspStatus.Error` e registra un errore, invece di restituire silenziosamente `IsValid = true`. L'abilitazione di OCSP nella configurazione rifiuterà ora tutti i certificati fino a quando non viene fornita un'implementazione reale, invece di accettarli silenziosamente.

---

#### 8. Autenticazione e Autorizzazione Abilitate per Impostazione Predefinita — Risolto

**File:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` e `EnableAuthorization` ora hanno entrambi il valore predefinito `true` nella classe `FeatureFlags`. `appsettings.json` imposta anche entrambi a `true`.

---

#### 9. Header di Sicurezza Applicati Prima del Routing — Risolto

**File:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` e `UseStandardSecurityHeaders` sono ora chiamati prima di `UseRouting`, `UseAuthentication` e `UseAuthorization`. Tutte le risposte, inclusi i cortocircuiti 401/403, ricevono gli header di sicurezza.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce nel Layout, Referrer-Policy — Risolto

**File:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Il cookie di sessione ora imposta `CookieSecurePolicy.Always` e `SameSiteMode.Strict`.
- L'header `Set-Cookie` senza nome malformato è stato rimosso.
- L'override globale `Content-Type: text/html` è stato rimosso.
- `AllowedHosts` in `appsettings.json` è ora `"localhost;127.0.0.1"`; il template usa `"{{YOUR_HOSTNAME}}"`.
- Tutti e tre i tag `<script>` in `_Layout.cshtml` includono ora `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` è ora aggiunto da `UseStandardSecurityHeaders`.

---

#### 16–19. Registrazione PII, Log della Stringa di Connessione, Stub Key Vault, X-XSS-Protection — Risolto

**File:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Tutti i PII (OID, e-mail, nome, SID, ruoli) sono ora sottoposti a hash HMAC-SHA256 tramite `LoggingHelper.HashPii()` prima di essere scritti nei log. Una chiave HMAC stabile può essere fornita tramite `Logging:PiiHmacKey` nella configurazione; viene utilizzata una chiave casuale per processo quando non configurata.
- L'istruzione di log di Cosmos DB ora conferma solo se una stringa di connessione è presente (`!string.IsNullOrEmpty`), non il suo contenuto.
- `AzureKeyVaultCertificateOperations` ora lancia `InvalidOperationException` all'avvio quando il certificato è null, invece di restituire silenziosamente valori fittizi.
- `X-XSS-Protection` è ora impostato a `"0"` (disabilitando il deprecato auditor XSS), coerente con le linee guida moderne dei browser.

---

## 🟠 Alto

### 20. NonceRefresherService Mantiene Dipendenze del Costruttore Key Vault Inutilizzate

**File:** `Services/NonceRefresherService.cs`

`NonceRefresherService` dichiara ancora parametri del costruttore per `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` e `IAzureKeyVaultOperationsService`. Poiché la generazione del nonce è stata semplificata per usare direttamente `RandomNumberGenerator`, nessuna di queste dipendenze viene utilizzata.

**Rischio:** Quando `EnableNonceServices = true` e `EnableKeyVault = false` (il predefinito), questi servizi non sono registrati nel contenitore DI, causando una `InvalidOperationException` in fase di esecuzione quando il servizio nonce viene risolto per la prima volta. Questa è effettivamente una condizione di denial-of-service innescata dalla configurazione predefinita. La classe `FeatureFlags` ha come predefinito `EnableNonceServices = true`, quindi qualsiasi ambiente che si affidi esclusivamente ai valori predefiniti della classe (senza override di `appsettings.json`) non riuscirebbe ad avviarsi.

**Raccomandazione:** Rimuovere i quattro parametri del costruttore inutilizzati e i loro campi privati corrispondenti da `NonceRefresherService`. Il servizio richiede solo `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`.

---

## 🟡 Medio

### 21. La Cache Interna di OcspValidationService Usa un Dictionary Non Thread-Safe

**File:** `Services/OcspValidationService.cs` (riga 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` non è thread-safe per letture e scritture concorrenti. Se `OcspValidationService` è registrato come singleton (o se la stessa istanza è condivisa tra le richieste da qualsiasi altro meccanismo), le validazioni OCSP concorrenti potrebbero corrompere la cache, causando voci perse, eccezioni lanciate o dati obsoleti restituiti.

**Raccomandazione:** Sostituire `Dictionary<string, CachedOcspResponse>` con `ConcurrentDictionary<string, CachedOcspResponse>`. Aggiornare la chiamata `_cache.Remove` (riga 103) a `_cache.TryRemove`.

---

## 🔵 Basso / Informativo

### 22. Stub di Validazione OCSP — Fallisce Chiuso ma Non Implementato

**File:** `Services/OcspValidationService.cs` (righe 157–173)

`PerformOcspValidationAsync` è ancora uno stub. La correzione del risultato #7 ha correttamente modificato il comportamento da "sempre valido" a "sempre non valido (fail-closed)". Tuttavia, il metodo non è ancora un'implementazione OCSP reale. Finché `EnableOcspValidation = false` (il predefinito), questo non ha impatto sulla produzione. Prima di abilitare OCSP in qualsiasi ambiente, deve essere implementato un client OCSP di qualità produzione.

---

### 23. mTLS con AllowedIssuers Vuoto Rifiuta Tutti i Certificati Client

**File:** `Models/Settings/MtlsSettings.cs`

Quando `ValidateClientCertificateIssuer = true` (il predefinito) e `AllowedIssuers` è vuoto (anche il predefinito quando non configurato), `IsIssuerAllowed()` restituisce `false`, causando il rifiuto di tutti i certificati client. Questo è il corretto comportamento fail-closed, ma non è documentato in modo prominente. Gli operatori che abilitano mTLS senza leggere attentamente il template potrebbero scoprire che tutte le connessioni client vengono rifiutate senza una spiegazione ovvia.

**Raccomandazione:** Aggiungere un messaggio di log di avviso all'avvio quando `ValidateClientCertificateIssuer = true` e `AllowedIssuers` è vuoto.

---

### 24. OcspSettings.ServerUnavailableBehavior Predefinito a "Warn"

**File:** `appsettings.template.json` (riga 134), `Services/OcspValidationService.cs`

L'impostazione `ServerUnavailableBehavior` ha come valore predefinito `"Warn"` nel template, il che consente alle richieste di passare quando il server OCSP non può essere raggiunto. Per ambienti ad alta sicurezza, questo dovrebbe essere `"Fail"` in modo che le interruzioni del server OCSP non degradino silenziosamente la verifica della revoca dei certificati.

**Raccomandazione:** Documentare chiaramente le tre opzioni (`Fail`, `Allow`, `Warn`) nel template e considerare di cambiare il valore predefinito a `"Fail"` per rispettare il principio del minimo privilegio.

---

## Valutazione degli Header di Sicurezza (Stato Attuale)

I seguenti header sono ora applicati tramite `UseStandardSecurityHeaders`:

| Header | Valore | Valutazione |
|--------|--------|-------------|
| `X-Frame-Options` | `DENY` | ✅ Buono |
| `X-XSS-Protection` | `0` | ✅ Buono (disabilita l'auditor deprecato) |
| `X-Content-Type-Options` | `nosniff` | ✅ Buono |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Buono |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Buono |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Buono |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Buono |
| `Permissions-Policy` | geolocalizzazione, fotocamera, microfono, interest-cohort disabilitati | ✅ Buono |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Buono |
| `Content-Security-Policy` | Basato su nonce, applicato quando CSP è abilitato | ✅ Buono |
| `Server` | Mascherato come `"webserver"` | ✅ Buono |
| `X-Powered-By` | Rimosso | ✅ Buono |

---

## Valutazione Generale

L'applicazione ha risolto tutte le vulnerabilità di gravità critica e alta dalla revisione precedente. I risultati attuali sono limitati a un problema di configurazione/DI di alta gravità (risultato #20) e elementi informativi di gravità inferiore. La postura di sicurezza è migliorata sostanzialmente. Si raccomanda un'azione immediata per il risultato #20 (dipendenze DI inutilizzate in NonceRefresherService), poiché può impedire l'avvio dell'applicazione con la configurazione predefinita.
