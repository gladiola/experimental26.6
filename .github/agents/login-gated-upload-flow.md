# Skill: login-gated-upload-flow

Add a proxdump-style authenticated upload experience: regular users upload through a gated
`[Authorize]` path, admins upload through a parallel `[Authorize(Policy = "AdminCertificate")]`
path, and anonymous visitors see a public cards feed. Established in
`gladiola/experimental26.6` PR #5 (`copilot/login-gated-upload`).

## What this skill adds

- **`RecordsController`** (`[Authorize(Policy = "AuthenticatedUser")]`) — user-facing CRUD:
  Index, Create (GET/POST), Details, Edit (GET/POST), Delete (GET/POST), Download
- **`AdminRecordsController`** (`[Authorize(Policy = "AdminCertificate")]`) — parallel admin
  upload path with the same Create flow but scoped to admin identity
- **`GroupAdminRecordsController`** (`[Authorize(Policy = "GroupAdminCertificate")]`) — group-
  scoped upload (see `group-scoped-access-tier` skill)
- **`HomeController.Index`** (`[AllowAnonymous]`) — public cards feed (only `IsPublic` records)
- **`UploadPolicy`** — static class that centralises upload rules (max size, allowed extensions,
  supported card types) so they are consistent across all upload paths

## Access model

| Route | Controller | Policy | Who can access |
|-------|-----------|--------|---------------|
| `/` | `HomeController` | `[AllowAnonymous]` | Everyone |
| `/Records` | `RecordsController` | `"AuthenticatedUser"` | Logged-in users |
| `/Admin/Records` | `AdminRecordsController` | `"AdminCertificate"` | Admin-cert holders |
| `/GroupAdmin/Records` | `GroupAdminRecordsController` | `"GroupAdminCertificate"` | Group-admin-cert holders |
| `/Records/download/{id}` | `RecordsController` | `[AllowAnonymous]` override | Owner, same-group, or public record |

## Files to create / modify

### `<Project>/Services/UploadPolicy.cs`

Centralise upload rules in a static class so they cannot diverge between controllers:

```csharp
public static class UploadPolicy
{
    public const long MaxUploadBytes = 50 * 1024 * 1024;  // 50 MB
    public static readonly IReadOnlyList<string> SupportedExtensions = new[] { ".json" };
    public static readonly IReadOnlyList<string> SupportedCardTypes  =
        new[] { "MIFARE Classic", "MIFARE Plus", "NTAG", "iCLASS", "HID Prox", "Other" };

    public static bool IsSupportedCardType(string? cardType) =>
        SupportedCardTypes.Contains(cardType ?? "", StringComparer.OrdinalIgnoreCase);

    public static async Task<byte[]> ReadFileBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
```

### `<Project>/Controllers/RecordsController.cs` — user upload

```csharp
[Authorize(Policy = "AuthenticatedUser")]
public class RecordsController : Controller
{
    // GET /Records
    public async Task<IActionResult> Index() { /* show own + same-group records */ }

    // GET /Records/upload
    public IActionResult Create()
    {
        ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;
        return View();
    }

    // POST /Records/upload
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Title,Description,CardType,IsPublic,UploadPermissionConfirmed")] CrudRecord input,
        IFormFile? uploadFile)
    {
        ViewData["SupportedCardTypes"] = UploadPolicy.SupportedCardTypes;
        // 1. Validate uploadFile is not null and extension is allowed
        // 2. Validate input.UploadPermissionConfirmed == true
        // 3. Validate UploadPolicy.IsSupportedCardType(input.CardType)
        // 4. Read bytes via UploadPolicy.ReadFileBytesAsync
        // 5. Build CrudRecord with OwnerId = UserIdentityHelper.GetStableUserId(User)
        //    and GroupId = groupAccessSettings.ResolveUserGroup(User, cert.Issuer)
        // 6. SaveChangesAsync; redirect to Index on success
        if (!ModelState.IsValid) return View(input);
        ...
        return RedirectToAction(nameof(Index));
    }

    // GET/POST /Records/details, edit, delete — guarded via FindAuthorizedRecordAsync
    // GET /Records/download/{id} — [AllowAnonymous] override; authenticated callers get
    //     resource-based read check; anonymous callers get IsPublic check
    [AllowAnonymous]
    public async Task<IActionResult> Download(string id) { ... }
}
```

### `<Project>/Controllers/AdminRecordsController.cs` — admin upload

Identical upload flow to `RecordsController` but gated by `"AdminCertificate"`:

```csharp
[Authorize(Policy = "AdminCertificate")]
public class AdminRecordsController : Controller
{
    public IActionResult Create() { ... }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(...) { ... }

    // Index, Details, Edit, Delete — same resource-based pattern; site admins pass all checks
}
```

Log all admin page accesses via `IAdminCertificateAuditService.LogPageAccess` in every action.

### `Views/Records/Create.cshtml` — upload form

Key form structure:

```html
<form method="post" enctype="multipart/form-data" asp-antiforgery="true">
    <input asp-for="Title" class="form-control" />
    <textarea asp-for="Description" class="form-control"></textarea>

    <select asp-for="CardType" asp-items="@(new SelectList(ViewData["SupportedCardTypes"]))">
        <option value="">— select a type —</option>
    </select>

    <input type="file" name="uploadFile" accept=".json" />

    <input asp-for="IsPublic" type="checkbox" />
    <input asp-for="UploadPermissionConfirmed" type="checkbox" />

    <button type="submit" class="btn btn-primary">Upload</button>
</form>
```

- `enctype="multipart/form-data"` is required for file upload
- `asp-antiforgery="true"` (or a `@Html.AntiForgeryToken()` call) emits the hidden CSRF token
- The `[ValidateAntiForgeryToken]` attribute on the POST action verifies it server-side

### Public cards feed (`HomeController.Index`)

```csharp
[AllowAnonymous]
public async Task<IActionResult> Index()
{
    var publicRecords = await _dbContext.CrudRecords
        .Where(r => r.IsPublic)
        .OrderByDescending(r => r.UpdatedUtc)
        .Take(50)
        .ToListAsync();
    return View(publicRecords);
}
```

The public feed shows only records with `IsPublic = true`, so no authentication check is needed.
Users wishing to upload must log in via the navigation link to `/Records/upload`.

## Upload validation checklist (apply in every upload action)

1. `uploadFile` is not null — `"Please choose a file"`
2. Extension is in `SupportedExtensions` — `"Only .json files are supported"`
3. `uploadFile.Length > 0` — `"The selected file is empty"`
4. `uploadFile.Length <= UploadPolicy.MaxUploadBytes` — `"Files larger than N MB are not allowed"`
5. `input.UploadPermissionConfirmed == true` — `"You must confirm upload permission"`
6. `UploadPolicy.IsSupportedCardType(input.CardType)` — `"Select a supported card type"`

Apply all checks before reading file bytes to avoid unnecessary memory allocation.

## `CrudRecord` properties set on upload

| Property | Source |
|----------|--------|
| `OwnerId` | `UserIdentityHelper.GetStableUserId(User)` |
| `OwnerDisplayName` | `UserIdentityHelper.GetDisplayName(User)` |
| `GroupId` | `GroupAccessSettings.ResolveUserGroup(User, cert.Issuer)` |
| `UploadedFileName` | `Path.GetFileName(uploadFile.FileName)` |
| `UploadedContentType` | `uploadFile.ContentType` (default `"application/octet-stream"`) |
| `UploadedFileSizeBytes` | `uploadFile.Length` |
| `UploadedFileContent` | bytes from `UploadPolicy.ReadFileBytesAsync` |
| `CreatedUtc` / `UpdatedUtc` | `DateTime.UtcNow` |

## `Program.cs` rate-limiting (recommended)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("RecordIdOperations", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});
```

Apply `[EnableRateLimiting("RecordIdOperations")]` to any action that accepts a record `{id}`
parameter (Details, Edit, Delete, Download) to slow brute-force enumeration.

## Source

`gladiola/experimental26.6` PR #5 (`copilot/login-gated-upload`).
