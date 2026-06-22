# Revisione della Sicurezza — WebAppExperimental266

**Data:** 2026-05-07
**Ambito:** Analisi statica completa del codice sorgente (follow-up alla revisione del 2026-05-06)
**Revisore:** Revisione della Sicurezza Automatizzata

---

## Sommario Esecutivo

Questa revisione di follow-up conferma che 3 delle 5 vulnerabilità identificate nella revisione della sicurezza del 2026-05-06 sono state completamente risolte, con 1 che rimane parzialmente risolta. La revisione identifica anche 4 nuovi riscontri. La postura di sicurezza complessiva dell'applicazione continua a migliorare.

---

## Stato dei Riscontri Precedenti (2026-05-06)

| # | Riscontro | Gravità | Stato |
|---|---------|----------|--------|
| 20 | NonceRefresherService mantiene dipendenze di costruttore Key Vault inutilizzate | 🟠 Alta | ✅ Risolto |
| 21 | La cache interna di OcspValidationService utilizza un Dictionary non thread-safe | 🟡 Media | ✅ Risolto |
| 22 | Lo stub di validazione OCSP è ancora presente — fallisce in modalità chiusa ma non implementato | 🔵 Bassa | ⚠️ Accettato (per progettazione) |
| 23 | mTLS con AllowedIssuers vuoto rifiuta tutti i certificati (fail-closed, non documentato) | 🔵 Bassa | ✅ Risolto |
| 24 | OcspSettings.ServerUnavailableBehavior è impostato per default a "Warn" (consente il passaggio in caso di errore) | 🔵 Bassa | ⚠️ Parzialmente Risolto |

---

## Stato Dettagliato dei Riscontri Precedenti

### ✅ 20. NonceRefresherService Dipendenze DI Inutilizzate — Risolto

**File:** `Services/NonceRefresherService.cs`

Il costruttore di `NonceRefresherService` ora dichiara soltanto `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`. Le quattro dipendenze precedentemente inutilizzate (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) sono state rimosse. Questo risolve il rischio di denial-of-service che impediva all'applicazione di avviarsi quando `EnableKeyVault = false` (il valore predefinito) e `EnableNonceServices = true` (il valore predefinito).

---

### ✅ 21. Cache Non Thread-Safe di OcspValidationService — Risolto

**File:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` è stato sostituito con `ConcurrentDictionary<string, CachedOcspResponse>`. La chiamata `_cache.Remove` è stata aggiornata a `_cache.TryRemove`. La cache è ora sicura per l'accesso concorrente.

---

### ⚠️ 22. Stub di Validazione OCSP — Accettato (Per Progettazione)

**File:** `Services/OcspValidationService.cs`

Lo stub è ancora presente ma fallisce correttamente in modalità chiusa. Poiché `EnableOcspValidation` è per default `false`, questo non ha alcun impatto in produzione. Questo è accettato come riscontro informativo in attesa di una implementazione OCSP completa.

---

### ✅ 23. mTLS AllowedIssuers Vuoto — Risolto

**File:** `Extensions/ServiceCollectionExtensions.cs`

Un avviso di avvio viene ora registrato quando `ValidateClientCertificateIssuer = true` e `AllowedIssuers` è vuoto:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Questo fornisce una guida chiara agli operatori che incontrano il comportamento fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Parzialmente Risolto

**File:** `appsettings.template.json` (risolto), `Models/Settings/OcspSettings.cs` (non ancora risolto)

Il template ora specifica correttamente `"ServerUnavailableBehavior": "Fail"`. Tuttavia, il valore predefinito della classe C# in `OcspSettings.cs` (riga 39) rimane `"Warn"`. Se un operatore abilita OCSP e omette `ServerUnavailableBehavior` dal proprio file di configurazione, il valore predefinito della classe `"Warn"` si applica silenziosamente, consentendo il passaggio durante i guasti del server OCSP. Il valore predefinito della classe dovrebbe essere modificato per corrispondere alla raccomandazione del template.

---

## Nuovi Riscontri

| # | Area | Gravità |
|---|------|----------|
| 25 | Il valore predefinito della classe OcspSettings ("Warn") diverge dal template ("Fail") | 🔵 Bassa |
| 26 | La chiave nonce condivisa unica di NonceCatalogService consente la collisione di nonce tra richieste | 🟡 Media |
| 27 | I contatori statici di OptimizedNonceMiddleware utilizzano interi con segno a 32 bit (rischio di overflow) | 🔵 Bassa |
| 28 | Program.cs registra un singleton ILoggerFactory vuoto, oscurando il logger del framework | 🟡 Media |

---

## 🟡 Media

### 26. La Chiave Nonce Condivisa di NonceCatalogService Consente la Collisione di Nonce tra Richieste

**File:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Il catalogo dei nonce memorizza tutti i nonce sotto un'unica chiave condivisa `"CSPNonce"`. Sotto carico concorrente, è possibile la seguente condizione di gara:

1. La richiesta A chiama `RefreshNonceAsync()` — il nonce A1 viene memorizzato come `_nonceCollection["CSPNonce"]`.
2. La richiesta B chiama `RefreshNonceAsync()` — il nonce B1 sovrascrive `_nonceCollection["CSPNonce"]`.
3. La richiesta A chiama `GetANonce("CSPNonce")` — riceve B1, non A1.
4. L'intestazione CSP e il nonce di layout della richiesta A contengono entrambi B1.
5. La richiesta B contiene anche B1.

Due risposte concorrenti condividono lo stesso nonce. Sebbene entrambi i valori siano ancora crittograficamente casuali e imprevedibili (nessuna stringa codificata), lo stesso valore di nonce appare in più risposte simultanee, indebolendo la garanzia di unicità per richiesta richiesta dalla specifica CSP. Un attaccante che può osservare il nonce di una risposta ha un nonce valido per almeno un'altra risposta concorrente.

**Raccomandazione:** Generare il nonce direttamente all'interno del middleware per richiesta (ad esempio, `Nonce.GenerateSecureNonce()`) e memorizzarlo solo in `HttpContext.Items["Nonce"]`, bypassando il catalogo condiviso per i nonce per richiesta. Il catalogo condiviso sarebbe quindi necessario solo se un nonce deve essere condiviso tra livelli di middleware all'interno di una singola richiesta, cosa che `HttpContext.Items` gestisce già nativamente.

---

### 28. Program.cs Registra un Singleton ILoggerFactory Vuoto

**File:** `Program.cs` (riga 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core registra automaticamente un `ILoggerFactory` completamente configurato (con tutti i provider di logging dalla configurazione `builder.Logging`) durante `WebApplication.CreateBuilder`. Questa registrazione esplicita `AddSingleton` aggiunge una seconda istanza `LoggerFactory` non configurata senza provider. Poiché `GetRequiredService<ILoggerFactory>()` restituisce l'implementazione registrata più di recente, i servizi che ricevono `ILoggerFactory` tramite dependency injection (come `NonceRefresherService`) utilizzeranno questa factory vuota e non produrranno alcun output di log tramite `_loggerFactory.CreateLogger<T>()`.

**Rischio:** Logging silenzioso in `NonceRefresherService` — i successi e i fallimenti della generazione di nonce non vengono emessi verso alcun sink di logging configurato. Questo riduce l'osservabilità dell'applicazione durante le operazioni sensibili alla sicurezza senza influire sulla funzionalità.

**Raccomandazione:** Rimuovere la registrazione esplicita `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. Il `ILoggerFactory` configurato del framework (con Console e qualsiasi altro provider) verrà quindi risolto correttamente dai servizi che ne dipendono.

---

## 🔵 Bassa / Informativo

### 25. Il Valore Predefinito della Classe OcspSettings Diverge dal Template

**File:** `Models/Settings/OcspSettings.cs` (riga 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Il template (`appsettings.template.json`) specifica `"ServerUnavailableBehavior": "Fail"`, ma il valore predefinito della classe C# è `"Warn"`. Se `ServerUnavailableBehavior` è assente dal file di configurazione attivo, il valore predefinito della classe si applica silenziosamente anziché la raccomandazione del template. Questo è un residuo del riscontro #24.

**Raccomandazione:** Cambiare il valore predefinito della classe da `"Warn"` a `"Fail"` per allinearlo con il template e il principio del minimo privilegio.

---

### 27. I Contatori Statici di OptimizedNonceMiddleware Possono Andare in Overflow

**File:** `Services/OptimizedNonceMiddleware.cs` (righe 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Questi contatori con segno a 32 bit vengono incrementati atomicamente tramite `Interlocked.Increment`. Dopo circa 2,1 miliardi di incrementi, si avvolgeranno a `int.MinValue` (−2.147.483.648), causando il calcolo dell'efficienza `(total - generated) * 100.0 / total` a produrre risultati errati o privi di significato. A 1.000 richieste al secondo, l'overflow si verifica dopo circa 24,8 giorni di operazione continua.

**Raccomandazione:** Cambiare i tipi di campo dei contatori da `int` a `long` e usare il sovraccarico `long` di `Interlocked.Increment` per prevenire l'overflow.

---

## Valutazione degli Header di Sicurezza (Stato Attuale)

I seguenti header vengono applicati tramite `UseStandardSecurityHeaders` — invariati rispetto alla revisione precedente:

| Header | Valore | Valutazione |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Buono |
| `X-XSS-Protection` | `0` | ✅ Buono (disabilita l'auditor obsoleto) |
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

## Valutazione Complessiva

Tutti i riscontri ad alta gravità delle revisioni precedenti sono stati risolti. I riscontri attuali si limitano a due problemi di gravità media (#26 chiave nonce condivisa, #28 ILoggerFactory vuoto) e due elementi informativi di bassa gravità (#25 discrepanza nel valore predefinito della classe, #27 overflow di interi nei contatori). Si raccomanda attenzione immediata per il riscontro #28 (singleton ILoggerFactory vuoto) poiché sopprime silenziosamente il logging diagnostico rilevante per la sicurezza durante le operazioni di nonce. Il riscontro #26 (chiave nonce condivisa) dovrebbe essere affrontato per ripristinare la garanzia di unicità di nonce per richiesta richiesta dalla specifica CSP.
