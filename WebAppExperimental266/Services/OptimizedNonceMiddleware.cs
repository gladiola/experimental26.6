using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Optimized middleware that generates nonces ONLY for HTML page responses,
    /// not for every HTTP request (static files, API calls, health checks, etc.)
    /// </summary>
    public class OptimizedNonceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly INonceRefresherService _nonceRefresherService;
        private readonly INonceCatalogService _nonceCatalogService;
        private readonly ILogger<OptimizedNonceMiddleware> _logger;

        // Paths that should NOT trigger nonce generation (static content, APIs)
        private static readonly string[] IgnorePaths = new[]
        {
            "/css", "/js", "/lib", "/images", "/fonts", "/wwwroot",
            "/favicon.ico", "/_framework", "/api", "/.well-known"
        };

        // Track nonce generation for monitoring
        private static int _nonceGenerationCount = 0;
        private static int _requestCount = 0;

        public OptimizedNonceMiddleware(
            RequestDelegate next,
            INonceRefresherService nonceRefresherService,
            INonceCatalogService nonceCatalogService,
            ILogger<OptimizedNonceMiddleware> logger)
        {
            _next = next;
            _nonceRefresherService = nonceRefresherService;
            _nonceCatalogService = nonceCatalogService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string caller = "OptimizedNonceMiddleware.InvokeAsync()";
            string nonce = string.Empty;
            Interlocked.Increment(ref _requestCount);

            // Check if this request should skip nonce generation
            if (ShouldIgnoreRequest(context.Request))
            {
                nonce = context.Items["Nonce"] as string ?? string.Empty;
                if (string.IsNullOrEmpty(nonce))
                {
                    nonce = Nonce.GenerateSecureNonce();
                }

                context.Items["Nonce"] = nonce;

                _logger.LogTrace("Reusing existing nonce for: {Path}", context.Request.Path);
            }
            else
            {
                // This is a page request - generate fresh nonce
                Interlocked.Increment(ref _nonceGenerationCount);

                _logger.LogDebug("Generating nonce #{Count}/{Total} for page: {Path}",
                    _nonceGenerationCount, _requestCount, context.Request.Path);

                try
                {
                    // Generate the nonce
                    nonce = await _nonceRefresherService.RefreshNonceAsync();

                    if (string.IsNullOrEmpty(nonce))
                    {
                        _logger.LogWarning("Nonce generation returned empty value for: {Path}", context.Request.Path);
                        // SECURITY: Never fall back to a predictable hardcoded string (Critical #3).
                        nonce = Nonce.GenerateSecureNonce();
                    }

                    LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "",
                        DataProcessingStatus.Info, $"Generated nonce for page request: {context.Request.Path}");

                    // Store the nonce in the HttpContext
                    context.Items["Nonce"] = nonce;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating nonce for: {Path}", context.Request.Path);

                    // SECURITY: Never fall back to a predictable hardcoded string (Critical #3).
                    // Produce a fresh random nonce even in the error path.
                    context.Items["Nonce"] = Nonce.GenerateSecureNonce();
                }
            }

            await _next(context);

            // Log efficiency metrics periodically
            if (_requestCount % 100 == 0)
            {
                var efficiency = (_requestCount - _nonceGenerationCount) * 100.0 / _requestCount;
                _logger.LogInformation(
                    "Nonce generation efficiency: {Efficiency:F1}% " +
                    "({Skipped}/{Total} requests skipped nonce generation)",
                    efficiency, _requestCount - _nonceGenerationCount, _requestCount);
            }
        }

        /// <summary>
        /// Determines if a request should skip nonce generation
        /// Returns true for static files, API calls, health checks, etc.
        /// </summary>
        private bool ShouldIgnoreRequest(HttpRequest request)
        {
            var path = request.Path.Value;
            string extension;
            bool result;

            if (string.IsNullOrEmpty(path))
            {
                result = false;
            }
            else
            {
                result = false;

                // Ignore requests to static file paths
                foreach (var ignorePath in IgnorePaths)
                {
                    if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                        break;
                    }
                }

                if (!result && path.Contains('.'))
                {
                    // Ignore requests with file extensions (static files)
                    // BUT allow .cshtml (Razor pages) and pages without extensions
                    extension = Path.GetExtension(path).ToLowerInvariant();

                    // These extensions need nonces (Razor pages)
                    if (extension == ".cshtml" || extension == string.Empty)
                    {
                        result = false;
                    }
                    else
                    {
                        // All other file extensions are static files
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get current nonce generation statistics
        /// </summary>
        public static (int NonceCount, int RequestCount, double EfficiencyPercent) GetStatistics()
        {
            var total = _requestCount;
            var generated = _nonceGenerationCount;
            var efficiency = total > 0 ? (total - generated) * 100.0 / total : 0;

            return (generated, total, efficiency);
        }
    }
}
