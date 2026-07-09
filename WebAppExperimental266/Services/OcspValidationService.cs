using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Interface for OCSP (Online Certificate Status Protocol) validation service
    /// </summary>
    public interface IOcspValidationService
    {
        /// <summary>
        /// Validates a certificate against an OCSP server
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <returns>True if certificate is valid, false if revoked or invalid</returns>
        Task<bool> ValidateCertificateAsync(X509Certificate2 certificate);

        /// <summary>
        /// Validates a certificate and returns detailed status
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <returns>Detailed validation result</returns>
        Task<OcspValidationResult> ValidateCertificateWithDetailsAsync(X509Certificate2 certificate);
    }

    /// <summary>
    /// Service for validating certificates using OCSP
    /// Template implementation - OCSP server must be implemented separately
    /// </summary>
    public class OcspValidationService : IOcspValidationService
    {
        private readonly ILogger<OcspValidationService> _logger;
        private readonly OcspSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, CachedOcspResponse> _cache;

        public OcspValidationService(
            ILogger<OcspValidationService> logger,
            OcspSettings settings,
            HttpClient httpClient)
        {
            _logger = logger;
            _settings = settings;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);
            _cache = new ConcurrentDictionary<string, CachedOcspResponse>();
        }

        /// <summary>
        /// Validates a certificate against the configured OCSP server
        /// </summary>
        public async Task<bool> ValidateCertificateAsync(X509Certificate2 certificate)
        {
            var result = await ValidateCertificateWithDetailsAsync(certificate);
            return result.IsValid;
        }

        /// <summary>
        /// Validates a certificate and returns detailed status information
        /// </summary>
        public async Task<OcspValidationResult> ValidateCertificateWithDetailsAsync(X509Certificate2 certificate)
        {
            var certKey = certificate.Thumbprint;
            OcspValidationResult result;

            // Check if OCSP validation is enabled
            if (!_settings.EnableOcspValidation)
            {
                if (_settings.EnableDetailedLogging)
                {
                    _logger.LogInformation("OCSP validation is disabled");
                }
                result = new OcspValidationResult
                {
                    IsValid = true,
                    Status = OcspStatus.Disabled,
                    Message = "OCSP validation is disabled"
                };
            }
            // Check if OCSP server URL is configured
            else if (string.IsNullOrEmpty(_settings.OcspServerUrl))
            {
                _logger.LogWarning("OCSP server URL is not configured");
                result = HandleServerUnavailable("OCSP server URL is not configured");
            }
            else
            {
                // Check cache first
                if (_cache.TryGetValue(certKey, out var cachedResponse))
                {
                    if (cachedResponse.ExpiresAt > DateTime.UtcNow)
                    {
                        if (_settings.EnableDetailedLogging)
                        {
                            _logger.LogInformation("Using cached OCSP response for certificate {Thumbprint}", certKey);
                        }
                        result = cachedResponse.Result;
                    }
                    else
                    {
                        // Remove expired cache entry
                        _cache.TryRemove(certKey, out _);
                        result = await PerformAndCacheOcspValidationAsync(certificate, certKey);
                    }
                }
                else
                {
                    result = await PerformAndCacheOcspValidationAsync(certificate, certKey);
                }
            }

            return result;
        }

        /// <summary>
        /// Performs OCSP validation and caches the result on success.
        /// </summary>
        private async Task<OcspValidationResult> PerformAndCacheOcspValidationAsync(X509Certificate2 certificate, string certKey)
        {
            OcspValidationResult result;

            // Perform OCSP validation
            try
            {
                result = await PerformOcspValidationAsync(certificate);

                // Cache the result
                if (result.IsValid && _settings.CacheDurationMinutes > 0)
                {
                    _cache[certKey] = new CachedOcspResponse
                    {
                        Result = result,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheDurationMinutes)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OCSP validation for certificate {Thumbprint}", certKey);
                result = HandleServerUnavailable($"OCSP validation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Performs the actual OCSP validation against the OCSP server.
        /// </summary>
        /// <remarks>
        /// This implementation is a stub. Until a real OCSP protocol implementation is
        /// provided, it fails closed (returns <c>IsValid = false</c>) so that enabling
        /// OCSP validation in configuration never silently passes revoked certificates.
        /// Replace this method with a production implementation before enabling OCSP in
        /// production environments.
        /// </remarks>
        private Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
        {
            _logger.LogError(
                "OCSP validation is enabled but no real implementation is present. " +
                "Certificate {Subject} ({Thumbprint}) is rejected until a production OCSP client is provided.",
                certificate.Subject, certificate.Thumbprint);

            return Task.FromResult(new OcspValidationResult
            {
                IsValid = false,
                Status = OcspStatus.Error,
                Message = "OCSP validation is not implemented. Replace PerformOcspValidationAsync with a production implementation.",
                ValidatedAt = DateTime.UtcNow,
                CertificateThumbprint = certificate.Thumbprint,
                CertificateSubject = certificate.Subject
            });
        }

        /// <summary>
        /// Handles the case when OCSP server is unavailable
        /// </summary>
        private OcspValidationResult HandleServerUnavailable(string message)
        {
            _logger.LogError("OCSP server unavailable - Rejecting request: {Message}", message);
            return new OcspValidationResult
            {
                IsValid = false,
                Status = OcspStatus.ServerUnavailable,
                Message = message
            };
        }
    }

    /// <summary>
    /// Result of OCSP certificate validation
    /// </summary>
    public class OcspValidationResult
    {
        public bool IsValid { get; set; }
        public OcspStatus Status { get; set; }
        public string? Message { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string? CertificateThumbprint { get; set; }
        public string? CertificateSubject { get; set; }
    }

    /// <summary>
    /// OCSP certificate status enumeration
    /// </summary>
    public enum OcspStatus
    {
        Good,           // Certificate is valid
        Revoked,        // Certificate has been revoked
        Unknown,        // Certificate status is unknown
        Disabled,       // OCSP validation is disabled
        ServerUnavailable, // OCSP server is not available
        Warning,        // Warning condition (server unavailable but allowing)
        Error           // Error during validation
    }

    /// <summary>
    /// Cached OCSP response
    /// </summary>
    internal class CachedOcspResponse
    {
        public required OcspValidationResult Result { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
