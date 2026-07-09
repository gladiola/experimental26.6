# Skill: group-scoped-access-tier

Add a fourth access tier — **group admin** — scoped to a single user group identified by CA
certificate issuer. Also enables same-group peer-read so users in the same group can view each
other's records while retaining owner-only mutation. Established in `gladiola/experimental26.6`
PR #9 (`copilot/group-admin-tier`).

## Four-tier role model

| Tier | Identity signal | What they can do |
|------|-----------------|-----------------|
| Anonymous | No login | View public records and public pages |
| Authenticated user | IdP login (e.g., Azure AD) | Create/edit/delete **own** records; read same-group peers' records |
| Group admin | IdP login + group-admin client certificate | Full CRUD on **all records in their group** via `GroupAdminRecordsController` |
| Site admin | IdP login + site-admin client certificate | Full CRUD on **all records** via `AdminRecordsController` |

## What this skill adds

- **`GroupAdminCertificateRequirement` / `GroupAdminCertificateRequirementHandler`** — validates
  that the client certificate maps the current user to a group-admin profile in `GroupAccessSettings`
- **`GroupAccessSettings`** — typed config: list of `GroupDefinition` objects (each with an ID,
  display name, issuer DN fragment, and a list of group-admin mappings) plus a flat
  `UserAssignments` list for explicit group membership overrides
- **`GroupAdminRecordsController`** — `[Authorize(Policy = "GroupAdminCertificate")]` controller
  that reads `GetCurrentGroupId()` from the certificate and scopes all DB queries to that group
- **`CrudRecord.GroupId`** — string field on the CRUD record that stores the owning group ID
- Peer-read in `CrudRecordAuthorizationHandler` — `Read` succeeds when the caller is in the same
  group as the record (see `resource-based-crud-idor-hardening` skill)

## Files to create / modify

### `<Project>/Models/Settings/GroupAccessSettings.cs`

Key types:

```csharp
public class GroupAccessSettings
{
    public List<GroupDefinition> Groups { get; set; } = new();
    public List<GroupUserAssignment> UserAssignments { get; set; } = new();

    // Returns the group ID for the current user, first by explicit assignment,
    // then by matching the client-certificate issuer against GroupDefinition.CertificateIssuerFragment.
    public string? ResolveUserGroup(ClaimsPrincipal user, string? certificateIssuer) { ... }

    // Returns a GroupAdminAuthorization object if the (user, thumbprint, issuer) triple
    // matches a configured group-admin mapping; otherwise null.
    public GroupAdminAuthorization? FindAuthorizedGroupAdmin(
        ClaimsPrincipal user, string thumbprint, string issuer) { ... }
}

public class GroupDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CertificateIssuerFragment { get; set; } = string.Empty;
    public List<GroupAdminUserMapping> GroupAdmins { get; set; } = new();

    public bool IsIssuerMatch(string issuer) =>
        !string.IsNullOrWhiteSpace(CertificateIssuerFragment) &&
        issuer.Contains(CertificateIssuerFragment, StringComparison.OrdinalIgnoreCase);
}

public class GroupAdminUserMapping
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> UserIdentifiers { get; set; } = new();
    public List<string> AllowedThumbprints { get; set; } = new();
}

public class GroupUserAssignment
{
    public string GroupId { get; set; } = string.Empty;
    public List<string> UserIdentifiers { get; set; } = new();
}

public class GroupAdminAuthorization
{
    public string GroupId { get; set; } = string.Empty;
    public string GroupDisplayName { get; set; } = string.Empty;
    public string AdminDisplayName { get; set; } = string.Empty;
}
```

### `<Project>/Services/GroupAdminCertificateRequirementHandler.cs`

Constructor dependencies: `IHttpContextAccessor`, `GroupAccessSettings`,
`IAdminCertificateAuditService`.

Validation:

1. Read `httpContext.Connection.ClientCertificate`; log and fail if null
2. Call `groupAccessSettings.FindAuthorizedGroupAdmin(user, cert.Thumbprint, cert.Issuer)`;
   log and fail if null
3. Log success (include `GroupDisplayName` and `AdminDisplayName`) and succeed

### `<Project>/Controllers/GroupAdminRecordsController.cs`

```csharp
[Authorize(Policy = "GroupAdminCertificate")]
public class GroupAdminRecordsController : Controller
{
    private string? GetCurrentGroupId()
    {
        var issuer = HttpContext.Connection.ClientCertificate?.Issuer;
        return _groupAccessSettings
            .FindAuthorizedGroupAdmin(User, HttpContext.Connection.ClientCertificate?.Thumbprint ?? "", issuer ?? "")
            ?.GroupId;
    }

    public async Task<IActionResult> Index()
    {
        var groupId = GetCurrentGroupId();
        if (groupId == null) return NotFound();
        var records = await _dbContext.CrudRecords
            .Where(r => r.GroupId == groupId)
            .OrderBy(r => r.OwnerDisplayName).ThenBy(r => r.Title)
            .ToListAsync();
        return View(records);
    }
    // Create, Edit, Delete follow the same group-scoped pattern
}
```

`AdminRouteNotFoundAuthorizationResultHandler` must include `"GroupAdminCertificate"` in its
policy name check so group-admin routes also return `404` on auth failure.

### `<Project>/Models/Main_Objects/CrudRecord.cs` — add `GroupId`

```csharp
public string GroupId { get; set; } = string.Empty;
```

Populate `GroupId` on record creation using `GroupAccessSettings.ResolveUserGroup(User, cert.Issuer)`.

### Peer-read in `CrudRecordAuthorizationHandler`

```csharp
var groupId = _groupAccessSettings.ResolveUserGroup(context.User, certificateIssuer);
var isInSameGroup = !string.IsNullOrWhiteSpace(groupId)
    && string.Equals(groupId, resource.GroupId, StringComparison.OrdinalIgnoreCase);

// Read is allowed for owner, public records, or same-group peers:
case nameof(CrudRecordOperations.Read):
    if (isOwner || resource.IsPublic || isInSameGroup) context.Succeed(requirement);
    break;
// Edit and Delete remain owner-only (and site-admin):
case nameof(CrudRecordOperations.Edit):
case nameof(CrudRecordOperations.Delete):
    if (isOwner) context.Succeed(requirement);
    break;
```

## Service registration

```csharp
// Inside AddAdminCertificateAuthorization (or a dedicated extension):
var groupAccessSettings = configuration.GetSection("GroupAccessSettings")
    .Get<GroupAccessSettings>() ?? new GroupAccessSettings();
services.AddSingleton(groupAccessSettings);
services.AddSingleton<IAuthorizationHandler, GroupAdminCertificateRequirementHandler>();

services.AddAuthorization(options =>
{
    options.AddPolicy("GroupAdminCertificate", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new GroupAdminCertificateRequirement()));
});
```

## `appsettings.json` sample

```json
{
  "GroupAccessSettings": {
    "Groups": [
      {
        "Id": "group-alpha",
        "DisplayName": "Team Alpha",
        "CertificateIssuerFragment": "CN=Alpha Intermediate CA",
        "GroupAdmins": [
          {
            "DisplayName": "Bob (Alpha Admin)",
            "UserIdentifiers": ["bob@myorg.example"],
            "AllowedThumbprints": ["AABBCCDDEEFF..."]
          }
        ]
      }
    ],
    "UserAssignments": [
      {
        "GroupId": "group-alpha",
        "UserIdentifiers": ["carol@myorg.example"]
      }
    ]
  }
}
```

## Group resolution precedence

1. Explicit `UserAssignments` entry for the user (overrides certificate-based detection)
2. `GroupDefinition.CertificateIssuerFragment` match against the client-certificate issuer DN

This lets you assign users to groups even when they do not present a certificate (e.g., only the
group admin has a cert, but regular group members are assigned by IdP identity).

## Source

`gladiola/experimental26.6` PR #9 (`copilot/group-admin-tier`).
