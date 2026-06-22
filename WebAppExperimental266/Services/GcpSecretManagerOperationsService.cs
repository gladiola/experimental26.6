using Google.Cloud.SecretManager.V1;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.GcpSecretManager;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// High-level service for fetching secrets and certificates from Google Cloud Secret Manager.
    /// Mirrors the role of <see cref="AzureKeyVaultOperationsService"/> for Azure Key Vault.
    /// </summary>
    public interface IGcpSecretManagerOperationsService
    {
        Task<AccessSecretVersionResponse> FetchSecret(string secretId);
        Task<X509Certificate2?> FetchCertificate();
        Task<AccessSecretVersionResponse> FetchSecretIVSecret();
        Task<AccessSecretVersionResponse> FetchSecretNonceKeySecret();
    }

    public class GcpSecretManagerOperationsService : IGcpSecretManagerOperationsService
    {
        private readonly ILogger<GcpSecretManagerOperationsService> _logger;
        private readonly IGcpSecretManagerSettingsService _settingsService;
        private readonly IGcpSecretManagerOperations _secretManagerOperations;

        public GcpSecretManagerOperationsService(
            ILogger<GcpSecretManagerOperationsService> logger,
            IGcpSecretManagerSettingsService settingsService,
            IGcpSecretManagerOperations secretManagerOperations)
        {
            _logger = logger;
            _settingsService = settingsService;
            _secretManagerOperations = secretManagerOperations;
        }

        public Task<AccessSecretVersionResponse> FetchSecret(string secretId)
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretManager(
                settings.ProjectId,
                secretId,
                string.IsNullOrEmpty(settings.CredentialFilePath) ? null : settings.CredentialFilePath);
        }

        public Task<X509Certificate2?> FetchCertificate()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetCertificateFromSecretManager(
                settings.ProjectId,
                settings.CertificateSecretId,
                certPasswordSecretId: string.Empty,
                string.IsNullOrEmpty(settings.CredentialFilePath) ? null : settings.CredentialFilePath);
        }

        public Task<AccessSecretVersionResponse> FetchSecretIVSecret()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretManager(
                settings.ProjectId,
                settings.IVSecretId,
                string.IsNullOrEmpty(settings.CredentialFilePath) ? null : settings.CredentialFilePath);
        }

        public Task<AccessSecretVersionResponse> FetchSecretNonceKeySecret()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretManager(
                settings.ProjectId,
                settings.NonceKeySecretId,
                string.IsNullOrEmpty(settings.CredentialFilePath) ? null : settings.CredentialFilePath);
        }
    }
}
