# Arotake Haumaru — WebAppExperimental266

**Rā:** 2026-05-05  
**Whānuitanga:** Tātaritanga tū-atu katoa o te waehere

---

## Ripanga Whakarāpopoto

| # | Wāhi | Taumaha |
|---|------|----------|
| 1 | Te whakamahi anō i te IV AES-GCM i te hanga nonce | 🔴 Tino Nui ✅ |
| 2 | I tuhia te nonce ki te tuhinga mārama | 🔴 Tino Nui ✅ |
| 3 | Ngā fallback nonce hardcoded | 🔴 Tino Nui ✅ |
| 4 | Papakupu nonce ā-ao kāore i te haumaru-muka | 🟠 Teitei |
| 5 | Kua comment-out te manatoko issuer mTLS | 🟠 Teitei |
| 6 | Kua whakaweto taunoa te tirotiro whakahokinga mTLS | 🟠 Teitei |
| 7 | Ka hoki tonu te OCSP ki te valid (stub) | 🟠 Teitei |
| 8 | Auth/authz kua whakaweto taunoa i te config | �� Teitei |
| 9 | Kua tōmuri rawa te whakamahi i ngā pane haumaru i te pipeline | 🟠 Teitei |
| 10 | Kāore te session cookie i whai Secure + SameSite | 🟡 Waenga |
| 11 | Pane `Set-Cookie` ā-ao hē te hanga | 🟡 Waenga |
| 12 | `Content-Type` kua whakahauhia ki `text/html` i ngā whakautu katoa | 🟡 Waenga |
| 13 | `AllowedHosts` he wildcard | 🟡 Waenga |
| 14 | Kāore i tāpirihia te nonce ki ngā tūtohu `<script>` i te layout | 🟡 Waenga |
| 15 | Kua ngaro te pane `Referrer-Policy` | 🟡 Waenga |
| 16 | PII i tuhia ki te tuhinga mārama | 🔵 Iti |
| 17 | Wāhanga connection string i roto i ngā logs | 🔵 Iti |
| 18 | Key Vault ops he stub | 🔵 Iti |
| 19 | `X-XSS-Protection` he pane tawhito | 🔵 Iti |

---

## 🔴 Tino Nui

### 1. Te whakamahi anō i te IV AES-GCM — Kua pakaru te whakamunatanga nonce ✅ Kua whakatika i te commit 45ae31b

**Kōnae:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

I whakamahia he IV pūmau i te AES-GCM i ia karanga. He hē nui tēnei i te whakamunatanga. Ko te nonce CSP
e tika ana kia hanga tika mā `RandomNumberGenerator.GetBytes(16)` me te Base64.

---

### 2. I tuhia ngā nonce ki te tuhinga mārama ✅ Kua whakatika i te commit bb6f27a

**Kōnae:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

I whakaputa tika ngā logs i te uara nonce. Mēnā ka āhei te tangata ki ngā logs, ka taea te karo i te CSP.

---

### 3. Ngā fallback nonce hardcoded ✅ Kua whakatika i te commit 11cc9f7

**Kōnae:** `Services/OptimizedNonceMiddleware.cs`

I whakamahia ngā aho fallback e mōhiotia ana (`bootstrap-nonce-placeholder`, `fallback-nonce`,
`error-fallback-nonce`). Ka taea te matapae, ka ngoikore te CSP.

---

## 🟠 Teitei

### 4. `NonceCatalogService` i whakamahi i tētahi `Dictionary` kāore i te haumaru-muka ✅ Kua whakatika i te commit ae2b6c9

**Kōnae:** `Services/NonceCatalogService.cs`

I whakamahia `Dictionary<TKey,TValue>` i tētahi horopaki concurrent. Me whakamahi `ConcurrentDictionary`.

---

### 5. Kua comment-out te manatoko issuer mTLS ✅ Kua whakatika i te commit fd3d4fb

**Kōnae:** `Extensions/ServiceCollectionExtensions.cs`

Ahakoa i tautuhia `ValidateClientCertificateIssuer`, i comment-out te waehere manatoko issuer.

---

### 6. Kua whakaweto taunoa te tirotiro whakahokinga mTLS ✅ Kua whakatika i te commit fd3d7b3

**Kōnae:** `Models/Settings/MtlsSettings.cs`, `appsettings.template.json`

Ko `CheckCertificateRevocation` i taunoa ki `false`.

---

### 7. He stub te OCSP, ka hoki tonu ki te valid ✅ Kua whakatika i te commit b4c3807

**Kōnae:** `Services/OcspValidationService.cs`

He implementation tātauira noa te `PerformOcspValidationAsync` i hoki ki `IsValid = true`.

---

### 8. Kua whakaweto taunoa te auth me te authorization ✅ Kua whakatika i te commit b392c47

**Kōnae:** `appsettings.json`

I tae mai te config taunoa me `EnableAzureAd = false` me `EnableAuthorization = false`.

---

### 9. Kua tōmuri te tāpiri pane haumaru ✅ Kua whakatika i te commit 016e57c

**Kōnae:** `Program.cs`

I tāpirihia ngā pane i muri i te routing/auth; tērā pea kāore ngā whakautu poto pērā i te 401/403 i whiwhi pane.

---

## 🟡 Waenga

### 10. Session cookie kāore i whai `Secure` me `SameSite` ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `Extensions/ServiceCollectionExtensions.cs`

---

### 11. Pane `Set-Cookie` ā-ao hē ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `Extensions/ApplicationBuilderExtensions.cs`

I tāpirihia he `Set-Cookie` kāore he ingoa/riu ki ia whakautu.

---

### 12. `Content-Type` i whakahauhia ki `text/html` ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `Extensions/ApplicationBuilderExtensions.cs`

I tukituki tēnei ki ngā API/binary/static-file response momo.

---

### 13. `AllowedHosts` he wildcard ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `appsettings.json`, `appsettings.template.json`

---

### 14. Kāore i whakamahia te nonce i ngā tūtohu `<script>` ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `Views/Shared/_Layout.cshtml`

---

### 15. Kua ngaro te `Referrer-Policy` ✅ Kua whakatika i te commit 8f2223c

**Kōnae:** `Extensions/ApplicationBuilderExtensions.cs`

---

## 🔵 Iti / Mōhiohio

### 16. PII i tuhia ki te tuhinga mārama ✅ Kua whakatika i te commit 93bb4e9

**Kōnae:** `Services/LoggingHelper.cs`

I tuhia rawatia ngā OID/īmēra/ingoa/SID/roles; i tūtohua te masking/hashing (HMAC-SHA256).

---

### 17. Wāhanga connection string i te log ✅ Kua whakatika i te commit 93bb4e9

**Kōnae:** `Extensions/ServiceCollectionExtensions.cs`

I whakaaturia ngā pūāhua whakamutunga o te secret.

---

### 18. Key Vault operations he stub ✅ Kua whakatika i te commit 93bb4e9

**Kōnae:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

---

### 19. `X-XSS-Protection: 1; mode=block` he pane tawhito ✅ Kua whakatika i te commit 93bb4e9

**Kōnae:** `Extensions/ApplicationBuilderExtensions.cs`

---
