# Gyaran Tsaro: An Rubuta Nonce a Rubutu Sarari (Critical #2)

**An gyara a:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

---

## Abin da ya faru ba daidai ba

An rubuta ƙimar nonce kai tsaye a logs a wurare biyu.

Misalin da ya kasance:

```csharp
$"Nonce: {nonce}"
```

da

```csharp
$"Generated Nonce: {CSPNonce}"
```

### Dalilin da ya sa wannan critical ne

CSP nonce sirri ce ta request guda. Duk wanda ya ga nonce a logs zai iya ƙirƙirar inline `<script>` mai nonce ɗin sannan ya ketare CSP.

---

## Abin da aka gyara

An maye gurbin saƙonnin da ke nuna nonce da saƙonnin status kawai:

- `"Nonce retrieved for request."`
- `"Nonce generated successfully."`

Babu ƙimar nonce da ake fitarwa a logs yanzu.

---

## Yadda za a ci gaba da gyaran

1. Kada a taba rubuta ƙimar nonce a logs
2. A duba duk sabbin logs a lambar nonce-related
3. Kada a saka nonce a telemetry/metrics/traces
4. A ɗauki nonce a matsayin sirri na request ɗaya

---

## Gwaje-gwajen da ke kare gyaran

- `NonceRefresherService_DoesNotLogNonceValue_OnSuccess`
- `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync`
