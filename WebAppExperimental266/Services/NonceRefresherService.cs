using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Services
{
    public interface INonceRefresherService {
        public Task<string> RefreshNonceAsync();
    }

    public class NonceRefresherService : INonceRefresherService
    {

        private readonly ILogger<NonceRefresherService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly INonceCatalogService _nonceCatalogService;

        public NonceRefresherService(ILogger<NonceRefresherService> logger, ILoggerFactory loggerFactory, INonceCatalogService nonceCatalogService) { 
        
            _logger = logger;
            _loggerFactory = loggerFactory;
            _nonceCatalogService = nonceCatalogService;

        }


        public Task<string> RefreshNonceAsync()
        {
            string caller = "NonceRefresherService.RefreshNonce()";
            string CSPNonce = string.Empty;
            ILogger<Nonce>? nonceLogger = null;
            Nonce? nonce = null;

            try
            {
                // Generate a fresh cryptographically random nonce using RandomNumberGenerator.
                // No Key Vault IV/key fetch is required — see Nonce.cs for the security rationale.
                nonceLogger = _loggerFactory.CreateLogger<Nonce>();
                nonce = new(nonceLogger);
                CSPNonce = nonce.GetNonceAsString();

                if (string.IsNullOrEmpty(CSPNonce))
                {
                    LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Failure, $"Did not generate Nonce.");
                }
                else
                {
                    // Log only success status — never log the nonce value (see Critical #2).
                    LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success, "Nonce generated successfully.");
                }
                _nonceCatalogService.AddANonce("CSPNonce", nonce);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Exception, ex.Message);
            }

            return Task.FromResult(CSPNonce);
        }
    }
}
