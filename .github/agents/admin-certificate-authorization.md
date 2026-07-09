# Skill: admin-certificate-authorization

Add per-admin X.509 certificate identity to an ASP.NET Core app so that admin routes require
both an authenticated primary identity and a pre-mapped client certificate. Established in
`gladiola/experimental26.6` PR #4 (core implementation), PR #8 (IDOR hardening + role
clarification), and PR #9 (group-admin tier).

## What this skill adds

- **`AdminCertificateRequirement` / `AdminCertificateRequirementHandler`** — validates that the
  TLS client certificate (a) comes from an allowed issuer and (b) is mapped to the currently
  authenticated user via a configurable thumbprint table
- **`AdminCertificateSettings`** — typed config: allowed issuer substrings + per-admin
  `{UserIdentifiers, AllowedThumbprints, DisplayName}` mappings
- **`IAdminCertificateAuditService` / `AdminCertificateAuditService`** — structured log sink for
  every certificate auth attempt and admin page access (success and failure)
- **`AdminRouteNotFoundAuthorizationResultHandler`** — implements
  `IAuthorizationMiddlewareResultHandler` to return `404 Not Found` instead of `403 Forbidden`
  on admin routes, preventing route enumeration
- **`"AdminCertificate"` authorization policy** — requires authenticated user plus
  `AdminCertificateRequirement`
- **`"GroupAdminCertificate"` authorization policy** — see the `group-scoped-access-tier` skill
  for the group-admin variant

## Files to create

### `<Project>/Models/Settings/AdminCertificateSettings.cs`

```csharp
public class AdminCertificateSettings
{
    public List<string> AllowedIssuers { get; set; } = new();
    public List<AdminCertificateUserMapping> AdminUsers { get; set; } = new();

    public bool IsIssuerAllowed(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer) || AllowedIssuers.Count == 0) return false;
        return AllowedIssuers.Any(a => issuer.Contains(a, StringComparison.OrdinalIgnoreCase));
    }

    public AdminCertificateUserMapping? FindAuthorizedAdmin(ClaimsPrincipal user, string thumbprint)
    {
        var normalized = NormalizeThumbprint(thumbprint);
        var identifiers = UserIdentityHelper.GetCandidateIdentifiers(user);
        return AdminUsers.FirstOrDefault(admin =>
            admin.UserIdentifiers.Any(id => identifiers.Contains(id, StringComparer.OrdinalIgnoreCase)) &&
            admin.AllowedThumbprints.Any(t =>
                string.Equals(NormalizeThumbprint(t), normalized, StringComparison.OrdinalIgnoreCase)));
    }

    public static string NormalizeThumbprint(string? t) =>
        string.IsNullOrWhiteSpace(t) ? string.Empty :
        t.Replace(":", "").Replace(" ", "").Trim().ToUpperInvariant();
}

public class AdminCertificateUserMapping
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> UserIdentifiers { get; set; } = new();
    public List<string> AllowedThumbprints { get; set; } = new();
}
```

### `<Project>/Services/AdminCertificateRequirementHandler.cs`

Constructor dependencies: `IHttpContextAccessor`, `AdminCertificateSettings`,
`IAdminCertificateAuditService`, `ILogger<AdminCertificateRequirementHandler>`.

Validation steps (fail fast):

1. Read `httpContext.Connection.ClientCertificate`; call `auditService.LogAuthenticationAttempt`
   with `success: false` and fail if null
2. Call `settings.IsIssuerAllowed(cert.Issuer)`; log and fail if issuer is not in the allow-list
3. Call `settings.FindAuthorizedAdmin(user, cert.Thumbprint)`; log and fail if no mapping found
4. Log success and call `context.Succeed(requirement)`

### `<Project>/Services/IAdminCertificateAuditService.cs`

Two methods:
- `void LogAuthenticationAttempt(HttpContext? ctx, ClaimsPrincipal user, bool success, string detail)`
- `void LogPageAccess(HttpContext? ctx, ClaimsPrincipal user, string pageName)`

Implement in `AdminCertificateAuditService` using structured `ILogger` calls. Sanitise all
string values (replace `\r`, `\n`, `\t` with `_`) before logging to prevent log-forging.

### `<Project>/Services/AdminRouteNotFoundAuthorizationResultHandler.cs`

```csharp
public class AdminRouteNotFoundAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && IsAdminCertificateEndpoint(context.GetEndpoint()))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await DefaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool IsAdminCertificateEndpoint(Endpoint? endpoint) =>
        endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(m => m.Policy is "AdminCertificate" or "GroupAdminCertificate") ?? false;
}
```

This handler is registered as a singleton `IAuthorizationMiddlewareResultHandler`. It intercepts
only `Forbidden` responses for admin-gated endpoints and converts them to `404`, so unauthenticated
callers cannot confirm which admin routes exist.

## Service registration (extension method)

```csharp
public static IServiceCollection AddAdminCertificateAuthorization(
    this IServiceCollection services, IConfiguration configuration, ILogger logger)
{
    var settings = configuration.GetSection("AdminCertificateSettings")
        .Get<AdminCertificateSettings>() ?? new AdminCertificateSettings();

    services.AddSingleton(settings);
    services.AddHttpContextAccessor();
    services.AddSingleton<IAdminCertificateAuditService, AdminCertificateAuditService>();
    services.AddSingleton<IAuthorizationHandler, AdminCertificateRequirementHandler>();
    services.AddSingleton<IAuthorizationMiddlewareResultHandler,
        AdminRouteNotFoundAuthorizationResultHandler>();

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminCertificate", policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new AdminCertificateRequirement()));
    });

    return services;
}
```

## Applying the policy to a controller

```csharp
[Authorize(Policy = "AdminCertificate")]
public class AdminRecordsController : Controller { ... }
```

Because `AdminRouteNotFoundAuthorizationResultHandler` is registered, a non-admin user hitting
`/AdminRecords` receives `404`, not `403`.

## Role-confusion guard: keep user and admin routes separate

Admin-cert identities must **not** be able to use ordinary user routes (`RecordsController`,
which requires `"AuthenticatedUser"`), and vice versa. Enforce this by:

- Keeping separate controller classes with separate policies
- Using `UserIdentityHelper.GetStableUserId` (a consistent, IdP-independent user identifier)
  on user routes; admin routes rely on the certificate mapping only
- Never mixing `[AllowAnonymous]` on individual actions of an `[Authorize(Policy = "AdminCertificate")]`
  controller (use a separate public controller instead)

## `appsettings.json` sample

```json
{
  "AdminCertificateSettings": {
    "AllowedIssuers": ["CN=OpenBSD Admin CA, O=MyOrg"],
    "AdminUsers": [
      {
        "DisplayName": "Alice (Site Admin)",
        "UserIdentifiers": ["alice@myorg.example"],
        "AllowedThumbprints": ["AABBCCDDEEFF..."]
      }
    ]
  }
}
```

## Kestrel prerequisite

The handler reads `httpContext.Connection.ClientCertificate`. Configure Kestrel to accept (not
require) client certificates so that non-admin routes remain accessible:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate);
});
```

## Source

`gladiola/experimental26.6` PR #4 (`copilot/add-crud-pages-admin-cert`),
PR #8 (`copilot/centralize-crud-auth`), PR #9 (`copilot/group-admin-tier`).
