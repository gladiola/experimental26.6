# Skill: localize-ui

Add full ASP.NET Core UI localization to an MVC app using the SharedResource pattern.
Established in `gladiola/eurocsv` PR #2 (34 locales), PR #7 (language selector), PR #8
(tooltip/advanced-options strings).

## What this skill adds

- `AddLocalization()` + `AddViewLocalization()` + `AddDataAnnotationsLocalization()` wired in DI
- `RequestLocalizationOptions` middleware with a default culture and supported culture list
- A `SharedResource` marker class and companion `.resx` files for all UI strings
- A `CultureController` that writes the ASP.NET Core culture cookie so users can switch languages
- A language-select partial view (`_LanguageSelect.cshtml`) rendered in the shared layout

## Files to create

### `<Project>/Resources/SharedResource.cs`

```csharp
namespace <Project>.Resources
{
    // Marker class — must live in the root namespace so ASP.NET Core resolves
    // Resources/SharedResource.{culture}.resx files via the fallback chain.
    public class SharedResource { }
}
```

The class has no members. Its namespace must match the assembly root namespace exactly.

### `<Project>/Resources/SharedResource.resx` (fallback / en-US)

The default `.resx` contains all keys with English values. Every supported culture gets its own
`SharedResource.<culture-code>.resx` (e.g., `SharedResource.de.resx`, `SharedResource.fr.resx`).
Region-specific cultures (e.g., `de-DE`, `de-AT`, `de-CH`) all fall back to the parent
`SharedResource.de.resx` — no per-region file is needed unless the translation differs.

### `<Project>/Controllers/CultureController.cs`

```csharp
[HttpPost]
public IActionResult SetCulture(string culture, string returnUrl)
{
    Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    return LocalRedirect(returnUrl);
}
```

Use `LocalRedirect` (not `Redirect`) to prevent open-redirect vulnerabilities.

### `<Project>/Views/Shared/_LanguageSelect.cshtml`

A `<form>` that POSTs to `CultureController.SetCulture` with a hidden `returnUrl` field.
Render one option per supported culture. Inject `IStringLocalizer<SharedResource>` for the
label if it needs to be translated.

## `Program.cs` changes

```csharp
// Service registration
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Middleware — place before UseHttpsRedirection
var supportedCultures = new[] { "en-US", "de-DE", "fr-FR", /* add all needed codes */ };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
```

## Injecting translations in views and controllers

```csharp
// In a controller or view:
@inject IStringLocalizer<SharedResource> Localizer
<p>@Localizer["WelcomeMessage"]</p>
```

## Supported locale list in eurocsv (34 locales)

```
en-US, en-GB,
de-DE, de-AT, de-CH,
fr-FR, fr-BE, fr-CH,
es-ES, es-MX,
it-IT,
nl-NL, nl-BE,
pt-PT, pt-BR,
pl-PL, cs-CZ, sk-SK, ru-RU,
sv-SE, da-DK, nb-NO, fi-FI,
hu-HU, ro-RO, tr-TR,
ja-JP, zh-CN,
el-GR, bg-BG, hr-HR, uk-UA, lt-LT, lv-LV
```

Copy `Resources/SharedResource.*.resx` from `gladiola/eurocsv` to inherit all translations.

## Tests to add

- Instantiate `IStringLocalizer<SharedResource>` via `WebApplicationFactory<Program>` and assert
  that representative keys resolve to non-empty strings for each supported culture
- Assert that `CultureController.SetCulture` with a non-local URL is rejected (open-redirect test)

## Source

`gladiola/eurocsv` PR #2 (`copilot/add-localization`), PR #7 (`copilot/improve-language-select`),
PR #8 (`copilot/localize-tooltips`).
