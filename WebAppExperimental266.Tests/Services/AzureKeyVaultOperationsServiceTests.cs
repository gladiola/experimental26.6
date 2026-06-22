using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Services;
using WebAppExperimental266.AzureKeyVaultOperations;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Services
{
    public class AzureKeyVaultOperationsServiceTests
    {
        private readonly Mock<ILogger<AzureKeyVaultOperationsService>> _mockLogger;
        private readonly Mock<IKeyVaultSettingsService> _mockKvSettings;
        private readonly Mock<IAzureADSettingsService> _mockAadSettings;
        private readonly Mock<INonceEncryptionSettingsService> _mockNonceSettings;
        private readonly Mock<IAzureKeyVaultCertificateOperations> _mockCertOps;
        private readonly AzureKeyVaultOperationsService _service;

        public AzureKeyVaultOperationsServiceTests()
        {
            _mockLogger = new Mock<ILogger<AzureKeyVaultOperationsService>>();
            _mockKvSettings = new Mock<IKeyVaultSettingsService>();
            _mockAadSettings = new Mock<IAzureADSettingsService>();
            _mockNonceSettings = new Mock<INonceEncryptionSettingsService>();
            _mockCertOps = new Mock<IAzureKeyVaultCertificateOperations>();

            _service = new AzureKeyVaultOperationsService(
                _mockLogger.Object,
                _mockKvSettings.Object,
                _mockAadSettings.Object,
                _mockNonceSettings.Object,
                _mockCertOps.Object);
        }

        [Fact]
        public void Constructor_ShouldAcceptAllDependencies()
        {
            // Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public async Task FetchCertificateServer_ShouldCallCertificateOperations()
        {
            // Arrange
            var kvSettings = new KeyVaultSettings
            {
                KeyVaultURL = "https://test-vault.vault.azure.net/",
                KeyVaultSecret = "test-cert",
                KeyVaultPassName = "test-pass"
            };
            var aadSettings = new AzureADSettings
            {
                Authority = "https://login.microsoftonline.com/test-tenant",
                Instance = "https://login.microsoftonline.com/",
                Domain = "test.onmicrosoft.com",
                TenantId = "test-tenant",
                ClientId = "test-client",
                ClientCredentials = new List<ClientCredential>()
            };

            _mockKvSettings.Setup(x => x.GetSettings()).Returns(kvSettings);
            _mockAadSettings.Setup(x => x.GetSettings()).Returns(aadSettings);

            // Act
            await _service.FetchCertificateServer();

            // Assert
            _mockCertOps.Verify(
                x => x.GetCertificateFromKeyVault(
                    aadSettings.TenantId,
                    aadSettings.ClientId,
                    kvSettings.KeyVaultURL,
                    kvSettings.KeyVaultSecret,
                    kvSettings.KeyVaultPassName),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecretIVSecret_ShouldCallGetSecret()
        {
            // Arrange
            var aadSettings = new AzureADSettings
            {
                Authority = "https://login.microsoftonline.com/test-tenant",
                Instance = "https://login.microsoftonline.com/",
                Domain = "test.onmicrosoft.com",
                TenantId = "test-tenant",
                ClientId = "test-client",
                ClientCredentials = new List<ClientCredential>
                {
                    new ClientCredential
                    {
                        SourceType = "ApplicationSecret",
                        ClientSecret = "test-secret"
                    }
                }
            };
            var nonceSettings = new NonceEncryptionSettings
            {
                KeyVaultURL = "https://test-vault.vault.azure.net/",
                IVSecret = "iv-secret",
                NonceKeySecret = "nonce-key-secret"
            };

            _mockAadSettings.Setup(x => x.GetSettings()).Returns(aadSettings);
            _mockNonceSettings.Setup(x => x.GetSettings()).Returns(nonceSettings);

            var expectedSecret = new KeyVaultSecret("iv-secret", "test-value");
            _mockCertOps.Setup(x => x.GetSecretFromKeyVault(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(expectedSecret);

            // Act
            var result = await _service.FetchSecretIVSecret();

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("iv-secret");
        }

        [Fact]
        public async Task FetchSecretNonceKeySecret_ShouldCallGetSecret()
        {
            // Arrange
            var aadSettings = new AzureADSettings
            {
                Authority = "https://login.microsoftonline.com/test-tenant",
                Instance = "https://login.microsoftonline.com/",
                Domain = "test.onmicrosoft.com",
                TenantId = "test-tenant",
                ClientId = "test-client",
                ClientCredentials = new List<ClientCredential>
                {
                    new ClientCredential
                    {
                        SourceType = "ApplicationSecret",
                        ClientSecret = "test-secret"
                    }
                }
            };
            var nonceSettings = new NonceEncryptionSettings
            {
                KeyVaultURL = "https://test-vault.vault.azure.net/",
                IVSecret = "iv-secret",
                NonceKeySecret = "nonce-key-secret"
            };

            _mockAadSettings.Setup(x => x.GetSettings()).Returns(aadSettings);
            _mockNonceSettings.Setup(x => x.GetSettings()).Returns(nonceSettings);

            var expectedSecret = new KeyVaultSecret("nonce-key-secret", "test-value");
            _mockCertOps.Setup(x => x.GetSecretFromKeyVault(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(expectedSecret);

            // Act
            var result = await _service.FetchSecretNonceKeySecret();

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("nonce-key-secret");
        }

        [Fact]
        public void FetchSecret_ShouldThrowNotImplementedException()
        {
            // Act
            Func<Task> act = async () => await _service.FetchSecret();

            // Assert
            act.Should().ThrowAsync<NotImplementedException>();
        }

        [Fact]
        public void FetchCertificate_ShouldThrowNotImplementedException()
        {
            // Act
            Func<Task> act = async () => await _service.FetchCertificate();

            // Assert
            act.Should().ThrowAsync<NotImplementedException>();
        }
    }
}
