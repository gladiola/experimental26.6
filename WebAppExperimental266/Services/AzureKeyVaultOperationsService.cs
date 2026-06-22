using Azure.Security.KeyVault.Secrets;
using WebAppExperimental266.AzureKeyVaultOperations;
using System.Security.Cryptography.X509Certificates;

namespace WebAppExperimental266.Services
{
    public interface IAzureKeyVaultOperationsService {
        public Task<KeyVaultSecret> FetchSecret();
        public Task<X509Certificate2?> FetchCertificate();
        public Task<X509Certificate2?> FetchCertificateServer();
        public Task<KeyVaultSecret> FetchSecretIVSecret();
        public Task<KeyVaultSecret> FetchSecretNonceKeySecret();
    }

    public class AzureKeyVaultOperationsService : IAzureKeyVaultOperationsService
    {
        private readonly ILogger<AzureKeyVaultOperationsService> _logger;
        private readonly IKeyVaultSettingsService _keyVaultSettingsService;
        private readonly IAzureADSettingsService _azureADSettingsService;
        private readonly INonceEncryptionSettingsService _nonceEncryptionSettingsService;
        private readonly IAzureKeyVaultCertificateOperations _azureKeyVaultCertificateOperations;

        public AzureKeyVaultOperationsService(
            ILogger<AzureKeyVaultOperationsService> logger, 
            IKeyVaultSettingsService keyVaultSettingsService, 
            IAzureADSettingsService azureADSettingsService, 
            INonceEncryptionSettingsService nonceEncryptionSettingsService, 
            IAzureKeyVaultCertificateOperations azureKeyVaultCertificateOperations)
        {
            _logger = logger;
            _keyVaultSettingsService = keyVaultSettingsService;
            _azureADSettingsService = azureADSettingsService;
            _nonceEncryptionSettingsService = nonceEncryptionSettingsService;
            _azureKeyVaultCertificateOperations = azureKeyVaultCertificateOperations;
        }

        public Task<KeyVaultSecret> FetchSecret()
        {
            throw new NotImplementedException();
        }

        public Task<X509Certificate2?> FetchCertificate()
        {
            throw new NotImplementedException();
        }

        public Task<X509Certificate2?> FetchCertificateServer()
        {
            string tenantId = _azureADSettingsService.GetSettings().TenantId;
            string clientId = _azureADSettingsService.GetSettings().ClientId;
            string keyVaultURL = _keyVaultSettingsService.GetSettings().KeyVaultURL;
            string certificateName = _keyVaultSettingsService.GetSettings().KeyVaultSecret;
            string certPasswordName = _keyVaultSettingsService.GetSettings().KeyVaultPassName;

            return _azureKeyVaultCertificateOperations.GetCertificateFromKeyVault(tenantId, clientId, keyVaultURL, certificateName, certPasswordName);
        }

        public Task<KeyVaultSecret> FetchSecretIVSecret()
        {
            string tenantId = _azureADSettingsService.GetSettings().TenantId;
            string clientId = _azureADSettingsService.GetSettings().ClientId;

            var clientSecretElement = _azureADSettingsService.GetSettings().ClientCredentials.FirstOrDefault(cc => cc.SourceType == "ApplicationSecret");
            string clientSecretValue = clientSecretElement?.ClientSecret! ?? String.Empty;

            string keyVaultURL = _nonceEncryptionSettingsService.GetSettings().KeyVaultURL;
            string ivSecret = _nonceEncryptionSettingsService.GetSettings().IVSecret;

            return _azureKeyVaultCertificateOperations.GetSecretFromKeyVault(tenantId, clientId, clientSecretValue, keyVaultURL, ivSecret);
        }

        public Task<KeyVaultSecret> FetchSecretNonceKeySecret()
        {
            string tenantId = _azureADSettingsService.GetSettings().TenantId;
            string clientId = _azureADSettingsService.GetSettings().ClientId;

            var clientSecretElement = _azureADSettingsService.GetSettings().ClientCredentials.FirstOrDefault(cc => cc.SourceType == "ApplicationSecret");
            string clientSecretValue = clientSecretElement?.ClientSecret! ?? String.Empty;

            string keyVaultURL = _nonceEncryptionSettingsService.GetSettings().KeyVaultURL;
            string nonceKeySecret = _nonceEncryptionSettingsService.GetSettings().NonceKeySecret;

            return _azureKeyVaultCertificateOperations.GetSecretFromKeyVault(tenantId, clientId, clientSecretValue, keyVaultURL, nonceKeySecret);
        }
    }
}
