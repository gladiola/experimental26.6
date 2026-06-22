using Amazon.SecretsManager.Model;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.AwsSecretManager;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// High-level service for fetching secrets and certificates from AWS Secrets Manager.
    /// Mirrors the role of <see cref="AzureKeyVaultOperationsService"/> for Azure Key Vault.
    /// </summary>
    public interface IAwsSecretsManagerOperationsService
    {
        Task<GetSecretValueResponse> FetchSecret(string secretName);
        Task<X509Certificate2?> FetchCertificate();
        Task<GetSecretValueResponse> FetchSecretIVSecret();
        Task<GetSecretValueResponse> FetchSecretNonceKeySecret();
    }

    public class AwsSecretsManagerOperationsService : IAwsSecretsManagerOperationsService
    {
        private readonly ILogger<AwsSecretsManagerOperationsService> _logger;
        private readonly IAwsSecretsManagerSettingsService _settingsService;
        private readonly IAwsSecretManagerOperations _secretManagerOperations;

        public AwsSecretsManagerOperationsService(
            ILogger<AwsSecretsManagerOperationsService> logger,
            IAwsSecretsManagerSettingsService settingsService,
            IAwsSecretManagerOperations secretManagerOperations)
        {
            _logger = logger;
            _settingsService = settingsService;
            _secretManagerOperations = secretManagerOperations;
        }

        public Task<GetSecretValueResponse> FetchSecret(string secretName)
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretsManager(
                settings.Region,
                settings.AccessKeyId,
                settings.SecretAccessKey,
                secretName);
        }

        public Task<X509Certificate2?> FetchCertificate()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetCertificateFromSecretsManager(
                settings.Region,
                settings.AccessKeyId,
                settings.SecretAccessKey,
                settings.CertificateSecretName,
                certPasswordSecretName: string.Empty);
        }

        public Task<GetSecretValueResponse> FetchSecretIVSecret()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretsManager(
                settings.Region,
                settings.AccessKeyId,
                settings.SecretAccessKey,
                settings.IVSecretName);
        }

        public Task<GetSecretValueResponse> FetchSecretNonceKeySecret()
        {
            var settings = _settingsService.GetSettings();
            return _secretManagerOperations.GetSecretFromSecretsManager(
                settings.Region,
                settings.AccessKeyId,
                settings.SecretAccessKey,
                settings.NonceKeySecretName);
        }
    }
}
