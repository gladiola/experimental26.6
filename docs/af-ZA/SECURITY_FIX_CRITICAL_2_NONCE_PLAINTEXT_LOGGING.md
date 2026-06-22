# Sekuriteitsregstelling: Nonce-waardes in Klarteks Aangeteken (Kritiek #2)

**Reggestel in:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Toetse:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Wat Was Fout

Twee liggings het die werklike CSP-nonce-waarde woordeliks in die toepassingslogstroom aangeteken:

**`Services/NonceMiddleware.cs` (reël 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (reël 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Waarom Dit Kritiek Is

'n CSP-nonce is die *enigste* meganisme wat inlynskrif-inspuiting voorkom sodra CSP afgedwing word. Die sekuriteit daarvan hang volledig af daarvan dat dit **geheim bly vir die lewensduur van 'n enkele respons**.

Toepassingslêers in 'n wolk-/ondernemingsomgewing is tipies leesbaar deur:
* Bedryfstiems
* Log-aggregasiedienste (bv. Azure Monitor, Splunk, ELK)
* Enige rekening met leestoegang tot die logslokdal

Enigeen wat 'n logreël wat `Nonce: <waarde>` bevat, kan lees, kan 'n inlyn `<script>`-etiket met daardie nonce-waarde inspuit en die blaaier sal dit uitvoer, wat CSP volledig omseil. Selfs as die nonce per versoek roteer, kan 'n aanvaller met regstreekse logtoegang binne dieselfde versoek se venster optree.

---

## Wat Reggestel Is

Beide logstellings is vervang met boodskappe wat die *status* van nonce-generering bevestig sonder om die waarde te openbaar:

**`NonceMiddleware.cs`:**
```csharp
// VOOR (kwesbaar):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// NA (veilig):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// VOOR (kwesbaar):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// NA (veilig):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Hoe om Dit Reggestel te Hou

1. **Teken die nonce-waarde nooit aan nie.** Logboodskappe kan bevestig dat 'n nonce gegenereer of gehaal is (sukses-/mislukkingstatus), maar die nonce-string self mag nooit in enige logparameter, gestruktureerde-aantekensveld of string-interpolasie verskyn nie.

2. **Hersien enige nuwe logstelling in nonce-verwante kode** (`NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) om te verseker dat die nonce-waarde nie ingesluit is nie.

3. **Moenie die nonce in telemetrie, statistieke of verspreide spore openbaar nie** om dieselfde redes. Spoorattribute en spanetikette word dikwels na log-aggregasie-agtereindes gestuur.

4. **Die nonce moet as 'n per-versoek-geheim behandel word.** Dit mag in `HttpContext.Items` gestoor word vir gebruik binne 'n enkele versoek se weergaweblomp, maar dit mag nie die proses verlaat via enige waarneembare kanaal behalwe die HTTP-responsopskrif en die `nonce="..."`-attribuut in die HTML wat dit beveilig nie.

### Toetse wat Hierdie Regstelling Afdwing

| Toets | Wat Dit Vang |
|-------|-------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Misluk as die nonce-string herbekendgestel word in enige logboodskap in `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Misluk as die nonce-string herbekendgestel word in enige logboodskap in `NonceMiddleware` |
