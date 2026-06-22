# सुरक्षा सुधार: Plaintext में nonce लॉग होना (Critical #2)

**सुधार किए गए फ़ाइलें:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## समस्या क्या थी

दो स्थानों पर वास्तविक CSP nonce मान log stream में plaintext में लिखे जा रहे थे।

- `NonceMiddleware.cs`
- `NonceRefresherService.cs`

Nonce यदि logs में दिखे तो log access वाला attacker उसी nonce के साथ inline script inject कर CSP bypass कर सकता है।

---

## क्या सुधारा गया

लॉग संदेशों को nonce value हटाकर status-only संदेशों में बदला गया:

- `"Nonce retrieved for request."`
- `"Nonce generated successfully."`

---

## इसे सुरक्षित कैसे रखें

1. nonce string को कभी भी logs/telemetry/traces में न लिखें
2. nonce-related कोड में नए log statements की समीक्षा करें
3. nonce को per-request secret मानें
4. nonce केवल response header/HTML nonce attribute में ही expose हो

---

## इसे enforce करने वाले tests

| Test | क्या रोकता है |
|---|---|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | refresher में nonce logging regression |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | middleware में nonce logging regression |
