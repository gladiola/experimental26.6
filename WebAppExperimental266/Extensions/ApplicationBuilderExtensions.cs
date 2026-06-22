using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Text;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;
using WebAppExperimental266.Interfaces.Main_Objects;

namespace WebAppExperimental266.Extensions
{
    public static class ApplicationBuilderExtensions
    {
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
                await app.ApplicationServices.GetRequiredService<INonceRefresherService>().RefreshNonceAsync();

                app.UseMiddleware<NonceMiddleware>();
                app.UseMiddleware<LoggingMiddleware>();

                app.Use(async (context, next) =>
                {
                    var nonceCatalog = app.ApplicationServices.GetRequiredService<INonceCatalogService>();
                    var cspNonce = nonceCatalog.GetANonce("CSPNonce");

                    // Use CSP Builder service
                    var cspBuilder = app.ApplicationServices.GetRequiredService<ContentSecurityPolicyBuilder>();
                    var cspSettings = app.ApplicationServices.GetService<IOptions<CSPScriptHashSettings>>()?.Value;

                    string cspHeader = cspBuilder.BuildCSPWithNonceAndHashes(
                        cspNonce,
                        cspSettings?.HashFilePath,
                        cspSettings);

                    context.Response.Headers.Append("Content-Security-Policy", cspHeader);

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
                    context.Response.Headers.Append("X-Frame-Options", "DENY");
                    context.Response.Headers.Append("X-XSS-Protection", "0");
                    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                    context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-site");
                    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=(), interest-cohort=()");

                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Append("Server", "webserver");
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");

                    context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                    context.Response.Headers.Append("Pragma", "no-cache");
                    context.Response.Headers.Append("Expires", "0");

                    await next.Invoke();
                });

                logger.LogInformation("Standard security headers configured");
            }

            return app;
        }
    }
}