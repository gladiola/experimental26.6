# Skill: resource-based-crud-idor-hardening

Consolidate CRUD record ownership/read-write authorization into a single resource-based handler
and harden against IDOR and route enumeration. Established in `gladiola/experimental26.6`
PR #8 (`copilot/centralize-crud-auth`).

## What this skill adds

- **`CrudRecordAuthorizationHandler`** — resource-based `AuthorizationHandler<OperationAuthorizationRequirement, CrudRecord>`
  that centralises all read/edit/delete access decisions in one place
- **`CrudRecordOperations`** — named `OperationAuthorizationRequirement` constants (`Read`,
  `Owner`, `Edit`, `Delete`) shared across controllers
- **`AdminRouteNotFoundAuthorizationResultHandler`** — returns `404` instead of `403` on
  admin-cert-gated routes to prevent route enumeration (see `admin-certificate-authorization` skill)
- A consistent `FindAuthorizedRecordAsync` helper on each controller that combines the DB fetch
  and the resource-based auth check into one call

## Access matrix

| Caller | Read | Edit | Delete |
|--------|------|------|--------|
| Site admin (valid admin cert) | ✓ | ✓ | ✓ |
| Record owner | ✓ | ✓ | ✓ |
| Same-group peer | ✓ (peer-read) | ✗ | ✗ |
| Public record (anonymous) | ✓ (via `IsPublic`) | ✗ | ✗ |
| Other authenticated user | ✗ | ✗ | ✗ |

## Files to create / modify

### `<Project>/Services/CrudRecordAuthorizationHandler.cs`

```csharp
public class CrudRecordAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, CrudRecord>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        CrudRecord resource)
    {
        // Admin short-circuit: site admins pass any requirement
        if (IsCertificateAdmin(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var isOwner = string.Equals(
            resource.OwnerId,
            UserIdentityHelper.GetStableUserId(context.User),
            StringComparison.OrdinalIgnoreCase);

        var groupId = _groupAccessSettings.ResolveUserGroup(
            context.User,
            _httpContextAccessor.HttpContext?.Connection.ClientCertificate?.Issuer);
        var isInSameGroup = !string.IsNullOrWhiteSpace(groupId)
            && string.Equals(groupId, resource.GroupId, StringComparison.OrdinalIgnoreCase);

        switch (requirement.Name)
        {
            case nameof(CrudRecordOperations.Read):
                if (isOwner || resource.IsPublic || isInSameGroup) context.Succeed(requirement);
                break;
            case nameof(CrudRecordOperations.Owner):
            case nameof(CrudRecordOperations.Edit):
            case nameof(CrudRecordOperations.Delete):
                if (isOwner) context.Succeed(requirement);
                break;
        }
        return Task.CompletedTask;
    }
}
```

Register as `services.AddSingleton<IAuthorizationHandler, CrudRecordAuthorizationHandler>()`.

### `<Project>/Services/CrudRecordOperations.cs`

```csharp
public static class CrudRecordOperations
{
    public static readonly OperationAuthorizationRequirement Read   = new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Owner  = new() { Name = nameof(Owner) };
    public static readonly OperationAuthorizationRequirement Edit   = new() { Name = nameof(Edit) };
    public static readonly OperationAuthorizationRequirement Delete = new() { Name = nameof(Delete) };
}
```

### Controller pattern: `FindAuthorizedRecordAsync`

Replace scattered `[Authorize]` checks on individual actions with a single private helper:

```csharp
private async Task<CrudRecord?> FindAuthorizedRecordAsync(
    string id,
    OperationAuthorizationRequirement requirement,
    string actionName)
{
    var record = await _dbContext.CrudRecords.FirstOrDefaultAsync(r => r.Id == id);
    if (record == null) return null;                       // real 404 — record does not exist

    var result = await _authorizationService.AuthorizeAsync(User, record, requirement);
    if (result.Succeeded) return record;

    // Log the ownership failure (sanitise values to prevent log-forging)
    _logger.LogWarning(
        "Auth failed for action {Action} record {RecordId} user {UserHash} owner {OwnerHash}",
        Sanitize(actionName), Sanitize(id),
        LoggingHelper.HashPii(UserIdentityHelper.GetStableUserId(User)),
        LoggingHelper.HashPii(record.OwnerId));

    return null;   // caller returns NotFound() — same response as a missing record
}

private static string Sanitize(string v) => v.Replace("\r", "").Replace("\n", "");
```

Key security properties:
- A **real** 404 (record truly absent) and a **synthetic** 404 (record exists but caller is not
  the owner) return the same HTTP status, preventing IDOR enumeration
- Ownership failure is **logged** with hashed PII but **not exposed** to the caller
- The record is fetched first to confirm existence; the auth check uses the live resource object,
  not a claim or URL parameter

### Action decoration

Controller actions should carry no ownership checks themselves — delegate entirely to the helper:

```csharp
// ✅ Good — ownership enforced once in FindAuthorizedRecordAsync
[HttpGet("edit/{id}")]
public async Task<IActionResult> Edit(string id)
{
    var record = await FindAuthorizedRecordAsync(id, CrudRecordOperations.Edit, "Edit");
    if (record == null) return NotFound();
    return View(record);
}

// ❌ Bad — scattered ownership check
[HttpGet("edit/{id}")]
public async Task<IActionResult> Edit(string id)
{
    var record = await _dbContext.CrudRecords.FindAsync(id);
    if (record == null || record.OwnerId != User.Identity!.Name) return NotFound();
    return View(record);
}
```

## Route enumeration defences

1. **Admin routes return 404, not 403** — register `AdminRouteNotFoundAuthorizationResultHandler`
   as `IAuthorizationMiddlewareResultHandler` (see `admin-certificate-authorization` skill)
2. **User-record routes return 404 on ownership mismatch** — enforced by `FindAuthorizedRecordAsync`
3. **Rate-limit record-ID operations** — apply `[EnableRateLimiting("RecordIdOperations")]` to
   any action that accepts a record ID parameter to slow brute-force enumeration attempts

## `Program.cs` registration

```csharp
// In the AddAdminCertificateAuthorization extension method (or equivalent):
services.AddSingleton<IAuthorizationHandler, CrudRecordAuthorizationHandler>();
```

Inject `IAuthorizationService` into controllers that call `FindAuthorizedRecordAsync`:

```csharp
public class RecordsController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    ...
}
```

## Tests to add

- Owner can Read, Edit, Delete their own record
- Non-owner authenticated user gets null (→ 404) for Edit and Delete
- Same-group peer can Read but not Edit or Delete
- Site admin passes all requirements
- Record that does not exist returns null (same surface as auth failure)

## Source

`gladiola/experimental26.6` PR #8 (`copilot/centralize-crud-auth`).
