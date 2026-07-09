# Skill: session-purge-setup

Add privacy-safe temporary file management with automatic background purging to an ASP.NET Core
app. Established in `gladiola/eurocsv` PR #6 (background timer), PR #11 (ContentRootPath fix),
and PR #12 (revert to `/tmp`).

## What this skill adds

- **`ITempFileService` / `TempFileService`** — saves uploaded and transformed files to a
  per-session directory under `/tmp/<appname>_sessions/<guid>/`; validates and sanitizes all
  session IDs and filenames; logs file creation and deletion events in a structured,
  log-forging-resistant format
- **`SessionPurgeService`** — `BackgroundService` that sweeps expired session directories on
  startup and then every 30 minutes; swallows exceptions to stay alive; fulfils the
  privacy-policy promise of "files deleted within N hours"

## Files to create

### `<Project>/Services/ITempFileService.cs` (or inline in `TempFileService.cs`)

Interface must expose:
- `Task<(string SessionId, string FilePath)> SaveUploadAsync(IFormFile file, CancellationToken ct)`
- `Task<string> SaveTransformedAsync(string sessionId, Stream content, string outputFileName, CancellationToken ct)`
- `string? GetOriginalPath(string sessionId)`
- `string? GetTransformedPath(string sessionId)`
- `void CleanupSession(string sessionId)`
- `void PurgeExpiredSessions(TimeSpan maxAge)`

### `<Project>/Services/TempFileService.cs`

Key implementation details:
- Base path: `/tmp/<appname>_sessions` — **do not** use `ContentRootPath`; that path may be
  read-only for `www-data` on Linux
- Session ID: `Guid.NewGuid().ToString("N")` (32 hex chars, no hyphens)
- Validate session ID: must be exactly 32 alphanumeric chars — reject anything else to prevent
  path traversal
- Sanitize file names with `Path.GetFileName` + replace invalid chars with `_`
- Sanitize log values: replace `\r`, `\n`, `\t` with `_` to prevent log-forging
- Directory structure per session:
  ```
  /tmp/<appname>_sessions/<sessionId>/
      original/   ← uploaded file
      transformed/ ← converted output
  ```
- Log structured events: `UserDeleteFile`, `UserDeleteCompleted`, `PurgeDeleteFile`,
  `PurgeDeleteSession`, `PurgeFailed` — all with sanitized paths

### `<Project>/Services/SessionPurgeService.cs`

```csharp
public class SessionPurgeService : BackgroundService
{
    public static readonly TimeSpan SessionMaxAge = TimeSpan.FromHours(2);
    public static readonly TimeSpan PurgeInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunPurge(); // immediate sweep on startup
        using var timer = new PeriodicTimer(PurgeInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            RunPurge();
    }

    private void RunPurge()
    {
        try { _tempFileService.PurgeExpiredSessions(SessionMaxAge); }
        catch (Exception ex) { _logger.LogError(ex, "PurgeSweepError"); }
    }
}
```

## `Program.cs` changes

```csharp
builder.Services.AddSingleton<ITempFileService, TempFileService>();
builder.Services.AddHostedService<SessionPurgeService>();
```

Do **not** do a one-shot purge at startup with `app.Services.CreateScope()` — the background
service handles that immediately on its first tick.

## Privacy-policy implications

With `SessionMaxAge = TimeSpan.FromHours(2)` and `PurgeInterval = TimeSpan.FromMinutes(30)`,
the worst-case retention is 2 h 30 min (files created just after a sweep). Adjust both constants
if the privacy policy states a different window.

## Tests to add

- `SessionMaxAge` and `PurgeInterval` are the expected values (constants test)
- `ExecuteAsync` calls purge at least once within 100 ms of start
- `ExecuteAsync` does not fault when `PurgeExpiredSessions` throws
- Use a `FakeTempFileService` stub that counts calls and records `maxAge` arguments

## Source

`gladiola/eurocsv` PR #6 (`copilot/session-purge-timer`), PR #11 (`copilot/fix-unauthorized-access`),
PR #12 (`copilot/revert-session-storage`).
