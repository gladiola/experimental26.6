# Skill: bootstrap-aspnet-mvc-app

Bootstrap a new ASP.NET Core MVC web application (net9.0) using the same structure
established in eurocsv (PR #1, branch `copilot/webappexperimental26`).

## What this skill does

Scaffolds a production-ready ASP.NET Core MVC app with:
- Serilog structured logging (console + hourly-rotating file sink, wrapped in `WriteTo.Async`)
- CSRF antiforgery
- Scoped/singleton service registration pattern
- Request size limits (50 MB default, configurable)
- `UseForwardedHeaders` for reverse-proxy compatibility
- Exception handler wired for both Development and Production
- Static assets + controller route with `.WithStaticAssets()`
- xUnit test project alongside the main project
- `public partial class Program { }` stub for integration test host access

## Steps

1. **Create solution and projects**
   ```
   dotnet new sln -n <AppName>
   dotnet new mvc -n <AppName> -f net9.0
   dotnet new xunit -n <AppName>.Tests -f net9.0
   dotnet sln add <AppName>/<AppName>.csproj
   dotnet sln add <AppName>.Tests/<AppName>.Tests.csproj
   ```

2. **Add NuGet packages to the main project**
   - `Serilog.AspNetCore` — host integration
   - `Serilog.Sinks.Async` — async sink wrapper (prevents interleaved log lines under load)

3. **`Program.cs` structure** — in this exact order:
   - Bootstrap `Log.Logger` via `LoggerConfiguration` before `WebApplication.CreateBuilder`
   - Call `builder.Host.UseSerilog(...)` using two-phase initialization
   - Register services (`AddLocalization`, `AddControllersWithViews`, `AddAntiforgery`, domain services)
   - Build `app`
   - `app.UseForwardedHeaders(...)` — **must come first**, before `UseHttpsRedirection`
   - `app.UseExceptionHandler("/Home/Error")` — wire for both environments
   - `app.UseRequestLocalization(...)` if applicable
   - `app.UseHttpsRedirection()`
   - Security middleware (if porting `port-security-headers` skill)
   - `app.UseSerilogRequestLogging(...)` with `TraceId` and `ClientIp` enrichment
   - `app.UseStaticFiles()`, `app.UseRouting()`, `app.UseAuthorization()`
   - `app.MapStaticAssets()`, `app.MapControllerRoute(...)` with `.WithStaticAssets()`
   - `app.Run()`
   - `public partial class Program { }` at the bottom

4. **Logging configuration** — use `WriteTo.Async(writeTo => writeTo.Console(...))` and
   `WriteTo.Async(writeTo => writeTo.File(...))` for both the bootstrap logger and the
   two-phase Serilog host config. File path: `logs/<appname>-.log`, `RollingInterval.Hour`,
   `retainedFileCountLimit: 720` (30 days).

5. **Test project** — reference the main project; use `public partial class Program` to host
   a `WebApplicationFactory<Program>` in integration tests.

6. **`.gitignore`** — add `logs/` to avoid committing log files.

## Source

Established in `gladiola/eurocsv` PR #1 (`copilot/webappexperimental26`).
For transferring to a new repo: copy `.github/agents/` from eurocsv or use eurocsv as a
GitHub Template Repository (Settings → "Template repository").
