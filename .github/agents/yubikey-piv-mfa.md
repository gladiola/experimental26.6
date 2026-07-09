# Skill: yubikey-piv-mfa

Add YubiKey PIV certificate MFA to an ASP.NET Core app using the OpenBSD admin-CA model.
Established in `gladiola/experimental26.6` PR #2 (core implementation) and PR #13
(YubiKey-gated Experimental records surface).

## What this skill adds

- **`YubiKeyRequirement` / `YubiKeyRequirementHandler`** — five-step certificate validation:
  certificate presence, issuer DN allow-list, CA thumbprint belt-and-suspenders check,
  optional YubiKey attestation-CA check, and live OCSP revocation check
- **`IOcspValidationService` / `OcspValidationService`** — HTTP-based OCSP client with response
  caching, configurable retry, and a `ServerUnavailableBehavior` fallback ("Fail", "Allow", "Warn")
- **`YubiKeySettings`** — typed config for allowed CA issuers, thumbprints, attestation toggle,
  and development OCSP bypass
- **`OcspSettings`** — typed config for OCSP server URL, timeout, retry, cache duration, and
  development bypass
- **`"YubiKeyMfa"` authorization policy** — requires authenticated user plus a successful
  `YubiKeyRequirement` evaluation
- **`FeatureFlags:EnableYubiKeyRequired`** — master toggle; when false the policy is not
  registered and the gate is entirely skipped

## Files to create

### `<Project>/Models/Settings/YubiKeySettings.cs`

Key properties:
| Property | Type | Purpose |
|----------|------|---------|
| `AllowedCaIssuers` | `List<string>` | Case-insensitive substring match against certificate `Issuer` DN |
| `AdminCaThumbprints` | `List<string>` | Optional SHA-256 thumbprints of trusted issuing CAs (colon/space-tolerant) |
| `RequireYubiKeyAttestation` | `bool` | When `true`, chain must include a cert whose issuer DN contains `YubiKeyAttestationIssuer` |
| `YubiKeyAttestationIssuer` | `string?` | DN fragment to match (default `"Yubico PIV Attestation"`) |
| `SkipOcspInDevelopment` | `bool` | Default `true`; skips OCSP in the `Development` environment |

Helper methods: `IsIssuerAllowed(string issuer)` and `IsThumbprintAllowed(string thumbprint)` —
both normalise the input (strip colons/spaces, case-insensitive) before comparing.

### `<Project>/Models/Settings/OcspSettings.cs`

Key properties: `EnableOcspValidation`, `OcspServerUrl`, `RequestTimeoutSeconds` (default 30),
`MaxRetryAttempts` (default 3), `CacheDurationMinutes` (default 60),
`ServerUnavailableBehavior` ("Warn" by default), `SkipValidationInDevelopment` (default `true`).

### `<Project>/Services/OcspValidationService.cs`

- Implement `IOcspValidationService` with `ValidateCertificateAsync(X509Certificate2)` and
  `ValidateCertificateWithDetailsAsync(X509Certificate2)`
- Cache responses in a `ConcurrentDictionary<string, CachedOcspResponse>` keyed on certificate
  serial number; evict entries older than `OcspSettings.CacheDurationMinutes`
- When `OcspSettings.EnableOcspValidation` is `false`, return `true` immediately (no network call)
- Apply `ServerUnavailableBehavior` when the OCSP server cannot be reached:
  - `"Fail"` → return `false` (reject request)
  - `"Allow"` → return `true` with a warning log
  - `"Warn"` (default) → return `true` with a warning log
- Register via `services.AddHttpClient("ocsp")` and inject through the factory

### `<Project>/Services/YubiKeyRequirementHandler.cs`

Constructor dependencies: `IHttpContextAccessor`, `YubiKeySettings`, `IOcspValidationService`,
`IWebHostEnvironment`, `ILogger<YubiKeyRequirementHandler>`.

Five-step validation (fail fast on each):

1. **Certificate present** — read from `httpContext.Connection.ClientCertificate`; fail if null
2. **Issuer allow-list** — call `YubiKeySettings.IsIssuerAllowed(cert.Issuer)`; fail if not matched
   (skip when `AllowedCaIssuers` is empty)
3. **CA thumbprint** — build the `X509Chain` with `RevocationMode = NoCheck`; walk elements
   `[1..]` (skip the leaf); fail if none of the CA thumbprints match
   (skip when `AdminCaThumbprints` is empty)
4. **YubiKey attestation** — when `RequireYubiKeyAttestation` is `true`, rebuild the chain and
   scan every element's `Issuer` for the `YubiKeyAttestationIssuer` fragment; fail if not found.
   **Note:** set `RequireYubiKeyAttestation = false` when using the admin-CA re-sign workflow
   (attestation is verified out-of-band at issuance, not at request time)
5. **OCSP revocation** — call `IOcspValidationService.ValidateCertificateAsync(cert)`; fail if
   `false`. Skip this step when `SkipOcspInDevelopment` is `true` and the environment is
   `Development`

Call `context.Succeed(requirement)` only after all applicable steps pass.

## `Program.cs` / service-registration changes

```csharp
// In AddFeatureFlags (or wherever FeatureFlags are read):
var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>() ?? new FeatureFlags();

if (flags.EnableYubiKeyRequired)
{
    builder.Services.AddYubiKeyMfaServices(builder.Configuration, logger);
}
```

`AddYubiKeyMfaServices` extension method (in `ServiceCollectionExtensions` or a dedicated extension):

```csharp
public static IServiceCollection AddYubiKeyMfaServices(
    this IServiceCollection services,
    IConfiguration configuration,
    ILogger logger)
{
    var yubiKeySettings = configuration.GetSection("YubiKeySettings").Get<YubiKeySettings>()
        ?? throw new InvalidOperationException("YubiKeySettings section is required when EnableYubiKeyRequired is true.");
    var ocspSettings = configuration.GetSection("OcspSettings").Get<OcspSettings>() ?? new OcspSettings();

    services.AddSingleton(yubiKeySettings);
    services.AddSingleton(ocspSettings);
    services.AddHttpContextAccessor();                                   // needed by handler
    services.AddHttpClient("ocsp");                                      // named client for OCSP
    services.AddSingleton<IOcspValidationService>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        return new OcspValidationService(
            sp.GetRequiredService<ILogger<OcspValidationService>>(),
            ocspSettings,
            factory.CreateClient("ocsp"));
    });
    services.AddSingleton<IAuthorizationHandler, YubiKeyRequirementHandler>();
    services.AddAuthorization(options =>
    {
        options.AddPolicy("YubiKeyMfa", policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new YubiKeyRequirement()));
    });
    return services;
}
```

## Gating a controller or route area

Apply `[Authorize(Policy = "YubiKeyMfa")]` at the controller level:

```csharp
[Authorize(Policy = "YubiKeyMfa")]
[Route("Experimental")]
public class ExperimentalRecordsController : Controller { ... }
```

For Razor Pages the policy can be applied to a folder in `AddRazorPages`:

```csharp
options.Conventions.AuthorizeFolder("/Experimental", "YubiKeyMfa");
```

## `appsettings.json` sample

```json
{
  "FeatureFlags": {
    "EnableYubiKeyRequired": true,
    "EnableOcspValidation": true
  },
  "YubiKeySettings": {
    "AllowedCaIssuers": ["CN=OpenBSD Admin CA, O=MyOrg"],
    "AdminCaThumbprints": [],
    "RequireYubiKeyAttestation": false,
    "YubiKeyAttestationIssuer": "Yubico PIV Attestation",
    "SkipOcspInDevelopment": true
  },
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "http://ocsp.myorg.example/ocsp",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "SkipValidationInDevelopment": true
  }
}
```

## CA model summary

The OpenBSD administrator is the sole CA. To grant access: sign a cert onto the user's YubiKey
PIV slot and distribute it (e.g., `yubico-piv-tool -a import-certificate`). To revoke access:
revoke the cert at the CA; OCSP propagates the revocation immediately. No application-side user
database is required — the CA *is* the account system.

## Kestrel prerequisite

The YubiKey handler reads `httpContext.Connection.ClientCertificate`. This is only populated when
Kestrel is configured to request or require a client certificate:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        // AllowCertificate lets non-YubiKey routes work without a cert
        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
    });
});
```

Pair this with `AddCertificate` (from `Microsoft.AspNetCore.Authentication.Certificate`) if you
want a dedicated mTLS scheme; for YubiKey-only MFA without full mTLS, reading the raw connection
certificate in the handler is sufficient.

## Source

`gladiola/experimental26.6` PR #2 (`copilot/add-yubikey-openbsd-mfa`) and
PR #13 (`copilot/yubikey-experimental-records`).
