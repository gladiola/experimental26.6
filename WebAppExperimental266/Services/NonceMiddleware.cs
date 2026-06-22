using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Services
{

    public class NonceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly INonceCatalogService _nonceCatalogService;
        private readonly ILogger<NonceMiddleware> _logger;
        private readonly INonceRefresherService _nonceRefresherService;
        private readonly IAzureADSettingsService _azureADSettingsService;

        public NonceMiddleware(RequestDelegate next, INonceCatalogService nonceCatalogService, ILogger<NonceMiddleware> logger, INonceRefresherService nonceRefresherService, IAzureADSettingsService azureADSettingsService)
        {
            _logger = logger;
            _next = next;
            _nonceCatalogService = nonceCatalogService;
            _nonceRefresherService = nonceRefresherService;
            _azureADSettingsService = azureADSettingsService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string caller = "NonceMiddleware.InvokeAsync()";
            // Generate the nonce
            await _nonceRefresherService.RefreshNonceAsync();
            var nonce = _nonceCatalogService.GetANonce("CSPNonce");

            // Do NOT log the nonce value — logging a nonce in plaintext allows anyone with log
            // access to inject inline scripts by spoofing the known nonce value (Critical #2).
            LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, "Nonce retrieved for request.");

            // Store the nonce in the HttpContext
            context.Items["Nonce"] = nonce;

            await _next(context);
        }
    }
}
