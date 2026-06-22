# የNonce ማመንጨት ማሻሻያ መመሪያ

## የአሁኑ ችግኝ

nonce አሁን በ**ሁሉም HTTP request** ይመነጫል፣ ይህም ያካትታል:
- static files (CSS/JS/ምስሎች)
- API calls
- health checks
- load balancer probes

ይህ እነዚህን ችግኞች ያመጣል:
- ብዙ የKey Vault ጥሪዎች
- አላስፈላጊ crypto ስራ
- የperformance ቅነሳ
- ከፍ የሚል ወጪ

## መፍትሔ፡ nonce ለHTML ምላሾች ብቻ

አዲስ nonce የሚያስፈልገው **HTML የሚያመጡ ምላሾች** ብቻ ነው።

---

## የአፈጻጸም አማራጮች

### አማራጭ 1: path filtering (ቀላል)
- `/css`, `/js`, `/lib`, `/api` እና extension ያላቸው requests እንዲተዉ
- nonce ለRazor/Page requests ብቻ

### አማራጭ 2: በresponse ጊዜ ማመንጨት (የሚመከር)
- `context.Response.OnStarting(...)` ይጠቀሙ
- `ContentType` ውስጥ `text/html` ካለ nonce ይፍጠሩ

### አማራጭ 3: lazy generation (ከፍተኛ ብቃት)
- CSP header ሲገነባ ብቻ nonce ይፍጠሩ
- አጭር ጊዜ caching + lock ይጠቀሙ

---

## የሚጠበቀው ውጤት

### ከማሻሻያ በፊት
- Requests/min: 1000
- Nonce generations: 1000
- Key Vault calls: 2000

### ከማሻሻያ በኋላ
- Requests/min: 1000
- Nonce generations: 100 (HTML ገፆች ብቻ)
- Key Vault calls: 200 (≈ 90% ቅነሳ)

---

## የተመከረ አቀራረብ

**Path Filtering + caching** ይጠቀሙ:
- የignore የpath ዝርዝር ይፍጠሩ
- static request ከሆነ pass-through ያድርጉ
- page request ከሆነ fresh nonce ይፍጠሩ
- nonce በ `HttpContext.Items["Nonce"]` ያስቀምጡ

---

## ሙከራ

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"              # nonce መፍጠር አለበት
Invoke-WebRequest "https://localhost:5001/css/site.css"  # nonce መፍጠር የለበትም
Invoke-WebRequest "https://localhost:5001/Privacy"       # nonce መፍጠር አለበት
```

nonce generation ቆጠራ ለማረጋገጥ logging ያክሉ።

---

## የሽግግር ዝርዝር

1. [ ] `NonceMiddleware.cs` ባክአፕ ያድርጉ
2. [ ] `OptimizedNonceMiddleware.cs` ይፍጠሩ
3. [ ] `Program.cs` ወደ አዲሱ middleware ያዘምኑ
4. [ ] static files ይሞክሩ
5. [ ] page requests ይሞክሩ
6. [ ] Azure Key Vault metrics ይከታተሉ
7. [ ] ማረጋገጫ በኋላ አሮጌ middleware ያስወግዱ

---

## አማራጭ ቅንብር

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

## የሚጠበቁ ጥቅሞች

- nonce generation ≈ 90% ቅነሳ
- Key Vault calls ≈ 90% ቅነሳ
- static content ፈጣን ምላሽ
- Azure ወጪ ቅነሳ
- ለHTML ገፆች ተመሳሳይ የደህንነት ደረጃ
