# Skill: arcgis-role-based-map

Embed an ArcGIS map panel on the home page with a distinct URL and label for each audience tier
(anonymous, authenticated user, site admin). Also covers the dynamic in-page record search/filter
UX introduced alongside it. Established in `gladiola/experimental26.6` PR #6 (ArcGIS panel +
dynamic filter) and PR #7 (per-role map selection).

## What this skill adds

- **`ArcGisSettings`** — typed config: three URL/label pairs (`Public*`, `User*`, `Admin*`)
- Per-role URL selection in `HomeController.Index` using `IAuthorizationService`
- `<iframe>` panel in the home view driven by `ViewData["ArcGisMapUrl"]` and
  `ViewData["ArcGisMapLabel"]`
- Dynamic in-page record list with client-side search/filter (`<input>` + JavaScript)
  for the public cards feed on the same home page

## Files to create / modify

### `<Project>/Models/Settings/ArcGisSettings.cs`

```csharp
public class ArcGisSettings
{
    private const string DefaultMapUrl =
        "https://harvard-cga.maps.arcgis.com/apps/mapviewer/index.html";

    public string PublicMapUrl   { get; set; } = DefaultMapUrl;
    public string PublicMapLabel { get; set; } = "Public View";
    public string UserMapUrl     { get; set; } = DefaultMapUrl;
    public string UserMapLabel   { get; set; } = "User View";
    public string AdminMapUrl    { get; set; } = DefaultMapUrl;
    public string AdminMapLabel  { get; set; } = "Admin View";
}
```

### `Program.cs` / service registration

```csharp
// Bind and register as singleton so it can be injected into controllers
var arcGisSettings = builder.Configuration.GetSection("ArcGisSettings")
    .Get<ArcGisSettings>() ?? new ArcGisSettings();
builder.Services.AddSingleton(arcGisSettings);
```

### `HomeController.cs` — role-aware URL selection

Inject `ArcGisSettings` and `IAuthorizationService`:

```csharp
[AllowAnonymous]
public async Task<IActionResult> Index()
{
    string mapUrl, mapLabel;

    if (User.Identity?.IsAuthenticated == true)
    {
        var adminResult = await _authorizationService.AuthorizeAsync(User, "AdminCertificate");
        if (adminResult.Succeeded)
        {
            mapUrl   = _arcGisSettings.AdminMapUrl;
            mapLabel = _arcGisSettings.AdminMapLabel;
        }
        else
        {
            mapUrl   = _arcGisSettings.UserMapUrl;
            mapLabel = _arcGisSettings.UserMapLabel;
        }
    }
    else
    {
        mapUrl   = _arcGisSettings.PublicMapUrl;
        mapLabel = _arcGisSettings.PublicMapLabel;
    }

    ViewData["ArcGisMapUrl"]   = mapUrl;
    ViewData["ArcGisMapLabel"] = mapLabel;

    // Public cards feed — pass to the view as the model
    var publicRecords = await _dbContext.CrudRecords
        .Where(r => r.IsPublic)
        .OrderByDescending(r => r.UpdatedUtc)
        .Take(50)
        .ToListAsync();

    return View(publicRecords);
}
```

The `"AdminCertificate"` policy check is the same one used by `AdminRecordsController`.
Unauthenticated callers always get the public map. No additional database query is needed for the
map selection itself.

### `Views/Home/Index.cshtml` — ArcGIS panel

```html
<div class="card mb-4">
    <div class="card-body">
        <h2 class="h5 mb-1">
            ArcGIS Plugin
            <span class="badge text-bg-secondary ms-1">@ViewData["ArcGisMapLabel"]</span>
        </h2>
        <a class="btn btn-sm btn-outline-secondary"
           href="@ViewData["ArcGisMapUrl"]"
           target="_blank" rel="noopener noreferrer">Open in new tab</a>
    </div>
    <iframe class="arcgis-plugin-frame"
            title="ArcGIS plugin — @ViewData["ArcGisMapLabel"]"
            src="@ViewData["ArcGisMapUrl"]"
            style="width:100%;height:480px;border:0;"
            loading="lazy"
            sandbox="allow-scripts allow-same-origin allow-forms allow-popups">
    </iframe>
</div>
```

Important `<iframe>` attributes:
- `sandbox` — restrict the embedded frame to the minimum permissions needed
- `loading="lazy"` — defer load until the frame is near the viewport
- `rel="noopener noreferrer"` on the fallback link

### `Views/Home/Index.cshtml` — dynamic record list with client-side filter

Below the ArcGIS panel, render the public cards feed with an in-page search input:

```html
<input id="record-filter" class="form-control mb-3"
       type="search" placeholder="Filter records…" aria-label="Filter records">

<div id="record-list">
  @foreach (var record in Model)
  {
    <div class="record-card" data-title="@record.Title.ToLowerInvariant()"
         data-type="@record.CardType?.ToLowerInvariant()">
      <h3>@record.Title</h3>
      <p>@record.Description</p>
    </div>
  }
</div>

<script nonce="@Context.Items["Nonce"]">
  document.getElementById('record-filter').addEventListener('input', function () {
    var term = this.value.toLowerCase();
    document.querySelectorAll('.record-card').forEach(function (card) {
      var match = !term
        || card.dataset.title.includes(term)
        || card.dataset.type.includes(term);
      card.style.display = match ? '' : 'none';
    });
  });
</script>
```

The `nonce` attribute is required when the `NonceMiddleware` from the `port-security-headers`
skill is active. If CSP is not in use, omit the attribute.

## `appsettings.json` sample

```json
{
  "ArcGisSettings": {
    "PublicMapUrl": "https://harvard-cga.maps.arcgis.com/apps/mapviewer/index.html",
    "PublicMapLabel": "Public View",
    "UserMapUrl": "https://myorg.maps.arcgis.com/apps/mapviewer/index.html?webmap=user123",
    "UserMapLabel": "Member View",
    "AdminMapUrl": "https://myorg.maps.arcgis.com/apps/mapviewer/index.html?webmap=admin456",
    "AdminMapLabel": "Admin View"
  }
}
```

## Adding more tiers

To add a group-admin tier, evaluate the `"GroupAdminCertificate"` policy before the user check:

```csharp
var groupAdminResult = await _authorizationService.AuthorizeAsync(User, "GroupAdminCertificate");
if (groupAdminResult.Succeeded) { mapUrl = ...; mapLabel = ...; }
else if (adminResult.Succeeded) { ... }
else { ... }
```

## Source

`gladiola/experimental26.6` PR #6 (`copilot/arcgis-dynamic-records`) and
PR #7 (`copilot/role-based-arcgis`).
