using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.GcpSecretManager;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class GcpSecretManagerOperationsServiceTests
    {
        private readonly Mock<ILogger<GcpSecretManagerOperationsService>> _mockLogger;
        private readonly Mock<IGcpSecretManagerSettingsService> _mockSettings;
        private readonly Mock<IGcpSecretManagerOperations> _mockOperations;
        private readonly GcpSecretManagerOperationsService _service;

        private static GcpSecretManagerSettings MakeSettings() => new GcpSecretManagerSettings
        {
            ProjectId = "my-project-123",
            CertificateSecretId = "server-cert",
            IVSecretId = "nonce-iv",
            NonceKeySecretId = "nonce-key",
            CredentialFilePath = string.Empty
        };

        private static AccessSecretVersionResponse MakeResponse(string projectId, string secretId) =>
            new AccessSecretVersionResponse
            {
                Name = new SecretVersionName(projectId, secretId, "latest").ToString(),
                Payload = new SecretPayload { Data = ByteString.Empty }
            };

        public GcpSecretManagerOperationsServiceTests()
        {
            _mockLogger = new Mock<ILogger<GcpSecretManagerOperationsService>>();
            _mockSettings = new Mock<IGcpSecretManagerSettingsService>();
            _mockOperations = new Mock<IGcpSecretManagerOperations>();
            _mockSettings.Setup(s => s.GetSettings()).Returns(MakeSettings());

            _service = new GcpSecretManagerOperationsService(
                _mockLogger.Object,
                _mockSettings.Object,
                _mockOperations.Object);
        }

        [Fact]
        public void Constructor_ShouldAcceptAllDependencies()
        {
            _service.Should().NotBeNull();
        }

        [Fact]
        public async Task FetchSecret_ShouldDelegateToOperations()
        {
            // Arrange
            var expected = MakeResponse("my-project-123", "my-secret");
            _mockOperations
                .Setup(o => o.GetSecretFromSecretManager("my-project-123", "my-secret", null))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecret("my-secret");

            // Assert
            result.Should().NotBeNull();
            _mockOperations.Verify(
                o => o.GetSecretFromSecretManager("my-project-123", "my-secret", null),
                Times.Once);
        }

        [Fact]
        public async Task FetchCertificate_ShouldDelegateToOperationsWithCertificateSecretId()
        {
            // Arrange
            _mockOperations
                .Setup(o => o.GetCertificateFromSecretManager(
                    "my-project-123", "server-cert", It.IsAny<string>(), null))
                .ReturnsAsync((System.Security.Cryptography.X509Certificates.X509Certificate2?)null);

            // Act
            var result = await _service.FetchCertificate();

            // Assert
            result.Should().BeNull();
            _mockOperations.Verify(
                o => o.GetCertificateFromSecretManager(
                    "my-project-123", "server-cert", It.IsAny<string>(), null),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecretIVSecret_ShouldUseIVSecretId()
        {
            // Arrange
            var expected = MakeResponse("my-project-123", "nonce-iv");
            _mockOperations
                .Setup(o => o.GetSecretFromSecretManager("my-project-123", "nonce-iv", null))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecretIVSecret();

            // Assert
            result.Should().NotBeNull();
            _mockOperations.Verify(
                o => o.GetSecretFromSecretManager("my-project-123", "nonce-iv", null),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecretNonceKeySecret_ShouldUseNonceKeySecretId()
        {
            // Arrange
            var expected = MakeResponse("my-project-123", "nonce-key");
            _mockOperations
                .Setup(o => o.GetSecretFromSecretManager("my-project-123", "nonce-key", null))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecretNonceKeySecret();

            // Assert
            result.Should().NotBeNull();
            _mockOperations.Verify(
                o => o.GetSecretFromSecretManager("my-project-123", "nonce-key", null),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecret_WithCredentialFilePath_PassesNonNullPath()
        {
            // Arrange — settings with a credential path
            var settingsWithCred = MakeSettings();
            settingsWithCred.CredentialFilePath = "/etc/gcp/key.json";
            _mockSettings.Setup(s => s.GetSettings()).Returns(settingsWithCred);

            _mockOperations
                .Setup(o => o.GetSecretFromSecretManager(
                    It.IsAny<string>(), It.IsAny<string>(), "/etc/gcp/key.json"))
                .ReturnsAsync(MakeResponse("my-project-123", "my-secret"));

            // Act
            await _service.FetchSecret("my-secret");

            // Assert
            _mockOperations.Verify(
                o => o.GetSecretFromSecretManager(
                    "my-project-123", "my-secret", "/etc/gcp/key.json"),
                Times.Once);
        }
    }
}
