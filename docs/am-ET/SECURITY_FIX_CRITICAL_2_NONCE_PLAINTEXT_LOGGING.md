# የደህንነት ጥገና፡ Nonce በግልጽ ጽሑፍ መመዝገብ (Critical #2)

**የተስተካከለበት:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

---

## ስህተቱ ምን ነበር

nonce ዋጋ በቀጥታ በlog ውስጥ ይታተም ነበር።

```csharp
$"Nonce: {nonce}"
```

```csharp
$"Generated Nonce: {CSPNonce}"
```

### ይህ ለምን critical ነው

CSP nonce የአንድ request ምስጢር ነው። በlog የተገኘ nonce በመጠቀም አጥቂ የinline `<script>` ጥቃት ሊያስገባ እና CSPን ሊያልፍ ይችላል።

---

## የተደረገው ጥገና

nonce ዋጋ የሚያሳዩ መልዕክቶች ተወግደው የstatus-only መልዕክቶች ተተኩ:

- `"Nonce retrieved for request."`
- `"Nonce generated successfully."`

አሁን nonce value በlog አይወጣም።

---

## ይህን ጥገና እንዴት እንደሚጠብቁ

1. nonce value በlog እንዳይጻፍ
2. nonce-related ኮድ ላይ የሚጨመሩ አዳዲስ logs እንዲገምገሙ
3. nonce በtelemetry/metrics/traces እንዳይገባ
4. nonce እንደ የአንድ request ምስጢር እንዲቆጠር

---

## ጥገናውን የሚጠብቁ ሙከራዎች

- `NonceRefresherService_DoesNotLogNonceValue_OnSuccess`
- `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync`
