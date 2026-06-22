# Jagorar Inganta Samar da Nonce

## Matsalar yanzu

Ana samar da nonce a **kowane HTTP request**, har da:
- static files (CSS/JS/hoto)
- API calls
- health checks
- load balancer probes

Wannan yana haifar da:
- Yawan kira zuwa Azure Key Vault
- Ƙarin aikin cryptography marar buƙata
- Raguwar aiki (performance)
- Ƙarin kuɗi

## Magani: Samar da nonce ga amsar HTML kawai

Samar da sabon nonce **ga amsoshin da za su fitar da HTML kawai** tare da CSP.

---

## Dabarun aiwatarwa

### Zaɓi 1: Tace ta request path (mai sauƙi)
- Tsallake `/css`, `/js`, `/lib`, `/api`, da requests masu extension
- Samar da nonce ga Razor/Page requests kawai

### Zaɓi 2: Nonce ɗaya ga kowace amsa (an fi so)
- Yi amfani da `context.Response.OnStarting(...)`
- Idan `ContentType` ya ƙunshi `text/html`, sannan a samar da nonce

### Zaɓi 3: Lazy generation (mafi inganci)
- Samar da nonce lokacin da ake gina CSP header kawai
- A yi caching mai ɗan lokaci tare da lock don guje wa ƙarin ƙirƙira

---

## Ingantawar da ake tsammani

### Kafin ingantawa
- Requests/min: 1000
- Nonce generations: 1000
- Key Vault calls: 2000

### Bayan ingantawa
- Requests/min: 1000
- Nonce generations: 100 (HTML pages kawai)
- Key Vault calls: 200 (kusan ragewa 90%)

---

## Shawarar aiwatarwa

Yi amfani da **Path Filtering + caching**:
- ƙirƙiri jerin hanyoyin da za a yi ignore
- idan request static ne, yi pass-through
- idan page request ne, samar da fresh nonce
- adana nonce a `HttpContext.Items["Nonce"]`

---

## Gwaji

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"              # ya kamata ya samar da nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # bai kamata ya samar da nonce ba
Invoke-WebRequest "https://localhost:5001/Privacy"       # ya kamata ya samar da nonce
```

Ƙara logging don ƙirga yawan nonce generation domin tabbatar da raguwar kira.

---

## Matakan hijira

1. [ ] Ajiye backup na `NonceMiddleware.cs`
2. [ ] Ƙirƙiri `OptimizedNonceMiddleware.cs`
3. [ ] Sabunta `Program.cs` don sabon middleware
4. [ ] Gwada static files
5. [ ] Gwada page requests
6. [ ] Saka idanu kan Azure Key Vault metrics
7. [ ] Cire tsohon middleware bayan tabbatarwa

---

## Saitin zaɓi

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

---

## Sakamakon da ake tsammani

- Rage nonce generation da kusan 90%
- Rage kiran Key Vault da kusan 90%
- Ingantaccen response time ga static content
- Rage kuɗin Azure
- Tsaro iri ɗaya ga shafukan HTML
