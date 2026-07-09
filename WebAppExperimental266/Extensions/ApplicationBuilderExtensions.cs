using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;
using WebAppExperimental266.Interfaces.Main_Objects;

namespace WebAppExperimental266.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly PathString[] RestrictedProbePaths =
        {
            new PathString("/healthz"),
            new PathString("/health"),
            new PathString("/ready"),
            new PathString("/alive")
        };

        /// <summary>
        /// Enable request localization middleware (culture negotiation via Accept-Language, query string, and cookie)
        /// </summary>
        public static IApplicationBuilder UseLocalizationConfiguration(
            this IApplicationBuilder app,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Request localization middleware is DISABLED");
            }
            else
            {
                var localizationOptions = app.ApplicationServices
                    .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
                app.UseRequestLocalization(localizationOptions);
                logger.LogInformation("Request localization middleware enabled");
            }

            return app;
        }

        /// <summary>
        /// Configure nonce middleware and CSP headers
        /// </summary>
        public static async Task<IApplicationBuilder> UseNonceAndSecurityHeadersAsync(
            this IApplicationBuilder app,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Nonce and CSP middleware is DISABLED");
            }
            else
            {
                app.UseMiddleware<NonceMiddleware>();
                app.UseMiddleware<LoggingMiddleware>();

                app.Use(async (context, next) =>
                {
                    var cspNonce = context.Items["Nonce"] as string;
                    if (string.IsNullOrWhiteSpace(cspNonce))
                    {
                        cspNonce = Nonce.GenerateSecureNonce();
                        context.Items["Nonce"] = cspNonce;
                    }

                    // Use CSP Builder service
                    var cspBuilder = app.ApplicationServices.GetRequiredService<ContentSecurityPolicyBuilder>();
                    var cspSettings = app.ApplicationServices.GetService<IOptions<CSPScriptHashSettings>>()?.Value;

                    string cspHeader = cspBuilder.BuildCSPWithNonceAndHashes(
                        cspNonce,
                        cspSettings?.HashFilePath,
                        cspSettings);

                    context.Response.Headers["Content-Security-Policy"] = cspHeader;

                    await next.Invoke();
                });

                logger.LogInformation("Nonce and CSP middleware configured");
            }

            return app;
        }

        /// <summary>
        /// Configure standard security headers
        /// </summary>
        public static IApplicationBuilder UseStandardSecurityHeaders(
            this IApplicationBuilder app,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Standard security headers are DISABLED");
            }
            else
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["X-XSS-Protection"] = "0";
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";
                    context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), interest-cohort=()";

                    context.Response.Headers.Remove("Server");
                    context.Response.Headers["Server"] = "webserver";
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");

                    if (!IsStaticAssetRequest(context.Request.Path))
                    {
                        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                        context.Response.Headers["Pragma"] = "no-cache";
                        context.Response.Headers["Expires"] = "0";
                    }

                    await next.Invoke();
                });

                logger.LogInformation("Standard security headers configured");
            }

            return app;
        }

        public static IApplicationBuilder UseRestrictedProbePaths(
            this IApplicationBuilder app,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                return app;
            }

            app.Use(async (context, next) =>
            {
                if (IsRestrictedProbePath(context.Request.Path) &&
                    context.User.Identity?.IsAuthenticated != true)
                {
                    logger.LogWarning(
                        "Rejected anonymous probe-path request for {Path}",
                        SanitizePath(context.Request.Path));
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next.Invoke();
            });

            return app;
        }

        private static bool IsStaticAssetRequest(PathString path)
        {
            if (!path.HasValue)
            {
                return false;
            }

            if (path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/images", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/fonts", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var extension = Path.GetExtension(path.Value);
            return !string.IsNullOrWhiteSpace(extension) &&
                   !string.Equals(extension, ".cshtml", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRestrictedProbePath(PathString path)
        {
            return RestrictedProbePaths.Contains(path);
        }

        private static string SanitizePath(PathString path)
        {
            return path.Value?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? "/";
        }
    }
}