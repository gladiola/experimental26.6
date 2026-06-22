# Nonce Generation Optimization Guide

## वर्तमान समस्या

इस समय nonce हर HTTP request पर generate हो रहा है, जिनमें static files, API calls और health checks भी शामिल हैं।
इससे अनावश्यक Key Vault calls, अतिरिक्त cryptographic work, और प्रदर्शन/लागत पर नकारात्मक प्रभाव पड़ता है।

## समाधान: केवल HTML response के लिए nonce

nonce केवल उन responses के लिए generate करें जिनमें HTML render होना है और CSP header आवश्यक है।

---

## मुख्य रणनीतियाँ

### विकल्प 1: Path Filtering (सरल)
`/css`, `/js`, `/lib`, `/api`, static file extension आदि paths के लिए nonce generation छोड़ दें।

### विकल्प 2: Response Pipeline (अनुशंसित)
`Response.OnStarting` में content-type जाँचकर (`text/html`) nonce generate करें।

### विकल्प 3: Lazy Generation (अधिक कुशल)
nonce तभी बनाएं जब CSP header build हो; सीमित lifetime के साथ reuse करें।

---

## अनुशंसित दृष्टिकोण

Path filtering + सुरक्षित fallback:
- ignore-path requests के लिए मौजूदा nonce (या सुरक्षित random fallback)
- page requests के लिए fresh nonce
- error path में hardcoded nonce कभी न रखें

---

## प्रदर्शन सुधार (उदाहरण)

**पहले:**
- 1000 requests/minute
- 1000 nonce generations
- 2000 Key Vault calls (IV + Key)

**बाद में:**
- 1000 requests/minute
- 100 nonce generations (केवल pages)
- 200 Key Vault calls

लगभग 90% कमी संभव।

---

## परीक्षण

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"                # nonce generate
Invoke-WebRequest "https://localhost:5001/css/site.css"    # nonce skip
Invoke-WebRequest "https://localhost:5001/Privacy"         # nonce generate
```

लॉगिंग से generation count ट्रैक करें और verify करें कि static traffic nonce trigger नहीं कर रहा।

---

## माइग्रेशन चरण

1. वर्तमान middleware का बैकअप लें
2. optimized middleware लागू करें
3. `Program.cs` registration अपडेट करें
4. static/page requests दोनों पर परीक्षण करें
5. Key Vault metrics मॉनिटर करें
6. सत्यापन के बाद पुराना middleware हटाएँ

---

## अपेक्षित परिणाम

- nonce generation में बड़ी कमी
- Key Vault calls में कमी
- static responses तेज़
- HTML pages के लिए समान सुरक्षा

---

## वैकल्पिक कॉन्फ़िगरेशन

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
