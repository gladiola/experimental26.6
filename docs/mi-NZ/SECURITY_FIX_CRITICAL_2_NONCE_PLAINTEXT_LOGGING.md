# Whakatika Haumaru: I tuhia ngā uara nonce ki te tuhinga mārama (Tino Nui #2)

**I whakatikaina ki:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Whakamātautau:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## He aha te raruraru

E rua ngā wāhi i tuhi tika i te nonce CSP ki ngā logs:

**`Services/NonceMiddleware.cs`**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs`**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

Ko te nonce te ārai matua mō te CSP ki te aukati i te script injection. Mēnā ka pānuihia i ngā logs,
ka taea te whakamahi e te kaiwhakaeke kia pahure te CSP i roto i taua matapihi tono.

---

## He aha i whakatikaina

Kua whakakapia ngā karere kia whakaatu i te **āhua** anake, kāore te uara nonce ake.

**`NonceMiddleware.cs`**
```csharp
// BEFORE:
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// AFTER:
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`**
```csharp
// BEFORE:
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// AFTER:
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Me pēhea te pupuri i te whakatika

1. **Kaua rawa e tuhi i te uara nonce.**
2. Arotakehia ngā log hou i `NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`,
   `NonceCatalogService` kia kore ai e maringi te nonce.
3. Kaua e tuku nonce ki telemetry/metrics/traces.
4. Me noho te nonce hei muna mō ia tono; ka tika te pupuri ki `HttpContext.Items` i roto i te tono kotahi.

### Ngā whakamātautau e here ana i tēnei whakatika

| Whakamātautau | He aha ka mau |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Ka hinga mēnā ka hoki mai te nonce ki ngā karere log a `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Ka hinga mēnā ka hoki mai te nonce ki ngā karere log a `NonceMiddleware` |
