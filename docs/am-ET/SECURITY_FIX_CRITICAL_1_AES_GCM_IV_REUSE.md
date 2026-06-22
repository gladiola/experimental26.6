# የደህንነት ጥገና፡ በNonce ማመንጨት ውስጥ AES-GCM IV ዳግም መጠቀም (Critical #1)

**የተስተካከለበት:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`

---

## ስህተቱ ምን ነበር

`Nonce` ክፍሉ በእያንዳንዱ ጥሪ ከKey Vault የሚመጣ **ቋሚ IV** ጋር AES-GCM ይጠቀም ነበር። በተመሳሳይ key + IV ዳግም መጠቀም ከባድ የክሪፕቶግራፊ ስህተት ነው።

- አጥቂ ሁለት ciphertext በማነጻጸር የplaintext ግንኙነት መረጃ ሊያገኝ ይችላል
- የauthentication tag ውሸት ማመንጨት ይቻላል

CSP nonce ለዚህ ጉዳይ ኢንክሪፕት መደረግ አያስፈልገውም፤ የሚያስፈልገው የማይታመን እና ለጥያቄ ልዩ መሆን ብቻ ነው።

---

## የተደረገው ጥገና

`Nonce.GenerateSecureNonce()` አሁን በቀጥታ `RandomNumberGenerator.Fill` ይጠቀማል:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

- ለnonce ማመንጨት የKey Vault IV/key አያስፈልግም
- AES-GCM ወይም ሌላ cipher አይጠቀምም
- `Nonce` constructor ከእንግዲህ `KeyVaultSecret` አይቀበልም

ተጨማሪ ጥገና፡ `NonceCatalogService.GetANonce` ውስጥ ያለው check-then-indexer ቅጥ ተወግዶ `TryGetValue(out ...)` በአንድ ደረጃ ተተካ።

---

## ይህን ጥገና እንዴት እንደሚጠብቁ

1. ለnonce ማመንጨት የKey Vault IV/key እንዳይመለስ
2. nonce generation ወደ AES-CBC/CTR/GCM እንዳይመለስ
3. nonce ቢያንስ 16 bytes እንዲቆይ
4. `RandomNumberGenerator.Fill` ወደ `new Random()` እንዳይቀየር
5. `GetANonce` ውስጥ `TryGetValue(out ...)` እንዲቀጥል

---

## ጥገናውን የሚጠብቁ ሙከራዎች

- `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters`
- `Nonce_GetNonceAsString_ReturnsNonEmptyBase64`
- `GenerateSecureNonce_Returns16ByteBase64`
- `Nonce_SuccessiveGenerations_AreUnique`
- `Nonce_HasSufficientEntropy`
- `NonceCatalogService_BackingStore_IsConcurrentDictionary`
- `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions`
