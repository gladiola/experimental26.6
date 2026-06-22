# Leitfaden zur Optimierung der Nonce-Erzeugung

## Aktuelles Problem

Derzeit wird bei **jeder HTTP-Anfrage** eine neue Nonce erzeugt, auch für:
- statische Dateien (CSS/JS/Bilder)
- API-Calls
- Health-Checks
- Load-Balancer-Probes

Das verursacht:
- unnötige Key-Vault-Aufrufe
- zusätzliche Kryptografie-Last
- schlechtere Performance
- höhere Cloud-Kosten

## Ziel

Nonce nur dort erzeugen, wo sie benötigt wird: bei HTML-Antworten mit CSP.

---

## Optimierungsansätze

### Option 1: Pfadfilter (einfach)

Nonce-Generierung für statische/API-Pfade überspringen (`/css`, `/js`, `/lib`, `/api`, Dateiendungen usw.).

### Option 2: Pro Antwort (empfohlen)

Nonce im Response-Pfad (z. B. `OnStarting`) nur für `text/html` generieren.

### Option 3: Lazy-Generierung (sehr effizient)

Nonce erst erzeugen, wenn CSP-Header gebaut wird; zusätzlich kurze Lebensdauer + Locking/Caching.

---

## Beispiel: Erwartete Wirkung

### Vorher
- 1.000 Requests/min
- 1.000 Nonce-Generierungen
- 2.000 Key-Vault-Operationen

### Nachher
- 1.000 Requests/min
- 100 Nonce-Generierungen (nur Seiten)
- 200 Key-Vault-Operationen

**=> ca. 90% Reduktion**

---

## Empfohlene Umsetzung

Kombiniere **Pfadfilter + Caching** in `OptimizedNonceMiddleware`:
- Ignorierliste für statische/API-Pfade
- vorhandene Nonce wiederverwenden, wo sinnvoll
- frische Nonce nur für echte Seitenanfragen
- robuste Fallbacks mit kryptografisch zufälliger Nonce

---

## Tests

### Funktionaler Test

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"                # Nonce erzeugen
Invoke-WebRequest "https://localhost:5001/css/site.css"     # keine neue Nonce
Invoke-WebRequest "https://localhost:5001/Privacy"          # Nonce erzeugen
```

### Beobachtbarkeit

Zähler/Logging hinzufügen, um erzeugte Nonces pro Pfadklasse zu messen.

---

## Migrationsschritte

1. Bestehende Middleware sichern
2. `OptimizedNonceMiddleware.cs` hinzufügen/anpassen
3. Registrierung in `Program.cs` umstellen
4. Statische Dateien und Seitenanfragen getrennt testen
5. Key-Vault-Metriken beobachten
6. Alte Middleware erst nach Verifikation entfernen

---

## Erwartetes Ergebnis

- deutlich weniger Nonce-Generierungen
- geringere Kosten und Latenz
- unverändertes Sicherheitsniveau für HTML/CSP

---

## Konfigurationsidee

```json
{
  "NonceGeneration": {
    "GenerateForStaticFiles": false,
    "GenerateForApiCalls": false,
    "NonceLifetimeMinutes": 5,
    "EnableOptimization": true
  }
}
```
