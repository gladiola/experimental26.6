using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.AwsSecretManager;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AwsSecretsManagerOperationsServiceTests
    {
        private readonly Mock<ILogger<AwsSecretsManagerOperationsService>> _mockLogger;
        private readonly Mock<IAwsSecretsManagerSettingsService> _mockSettings;
        private readonly Mock<IAwsSecretManagerOperations> _mockOperations;
        private readonly AwsSecretsManagerOperationsService _service;

        private static AwsSecretsManagerSettings MakeSettings() => new AwsSecretsManagerSettings
        {
            Region = "us-east-1",
            AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
            SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            CertificateSecretName = "server-cert",
            IVSecretName = "nonce-iv",
            NonceKeySecretName = "nonce-key"
        };

        public AwsSecretsManagerOperationsServiceTests()
        {
            _mockLogger = new Mock<ILogger<AwsSecretsManagerOperationsService>>();
            _mockSettings = new Mock<IAwsSecretsManagerSettingsService>();
            _mockOperations = new Mock<IAwsSecretManagerOperations>();
            _mockSettings.Setup(s => s.GetSettings()).Returns(MakeSettings());

            _service = new AwsSecretsManagerOperationsService(
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
            var expected = new GetSecretValueResponse { Name = "my-secret", SecretString = "value" };
            _mockOperations
                .Setup(o => o.GetSecretFromSecretsManager(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "my-secret"))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecret("my-secret");

            // Assert
            result.Name.Should().Be("my-secret");
            _mockOperations.Verify(
                o => o.GetSecretFromSecretsManager(
                    "us-east-1",
                    "AKIAIOSFODNN7EXAMPLE",
                    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                    "my-secret"),
                Times.Once);
        }

        [Fact]
        public async Task FetchCertificate_ShouldDelegateToOperationsWithCertificateSecretName()
        {
            // Arrange
            _mockOperations
                .Setup(o => o.GetCertificateFromSecretsManager(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    "server-cert", It.IsAny<string>()))
                .ReturnsAsync((System.Security.Cryptography.X509Certificates.X509Certificate2?)null);

            // Act
            var result = await _service.FetchCertificate();

            // Assert
            result.Should().BeNull();
            _mockOperations.Verify(
                o => o.GetCertificateFromSecretsManager(
                    "us-east-1",
                    "AKIAIOSFODNN7EXAMPLE",
                    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                    "server-cert",
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecretIVSecret_ShouldUseIVSecretName()
        {
            // Arrange
            var expected = new GetSecretValueResponse { Name = "nonce-iv", SecretString = "iv-value" };
            _mockOperations
                .Setup(o => o.GetSecretFromSecretsManager(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "nonce-iv"))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecretIVSecret();

            // Assert
            result.Name.Should().Be("nonce-iv");
            _mockOperations.Verify(
                o => o.GetSecretFromSecretsManager(
                    "us-east-1",
                    It.IsAny<string>(), It.IsAny<string>(), "nonce-iv"),
                Times.Once);
        }

        [Fact]
        public async Task FetchSecretNonceKeySecret_ShouldUseNonceKeySecretName()
        {
            // Arrange
            var expected = new GetSecretValueResponse { Name = "nonce-key", SecretString = "key-value" };
            _mockOperations
                .Setup(o => o.GetSecretFromSecretsManager(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "nonce-key"))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.FetchSecretNonceKeySecret();

            // Assert
            result.Name.Should().Be("nonce-key");
            _mockOperations.Verify(
                o => o.GetSecretFromSecretsManager(
                    "us-east-1",
                    It.IsAny<string>(), It.IsAny<string>(), "nonce-key"),
                Times.Once);
        }
    }
}
