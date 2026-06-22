using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.GcpSecretManager;

namespace WebAppExperimental266.Tests.Services
{
    public class GcpSecretManagerOperationsTests
    {
        private readonly Mock<ILogger<GcpSecretManagerOperations>> _mockLogger;
        private readonly GcpSecretManagerOperations _operations;

        public GcpSecretManagerOperationsTests()
        {
            _mockLogger = new Mock<ILogger<GcpSecretManagerOperations>>();
            _operations = new GcpSecretManagerOperations(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldAcceptLogger()
        {
            _operations.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCertificateFromSecretManager_TemplateImplementation_LogsWarning()
        {
            // Act
            var result = await _operations.GetCertificateFromSecretManager(
                "my-project", "cert-secret-id", "cert-pass-id");

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetCertificateFromSecretManager")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSecretFromSecretManager_TemplateImplementation_ReturnsResponse()
        {
            // Act
            var result = await _operations.GetSecretFromSecretManager("my-project", "my-secret-id");

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Contain("my-secret-id");
            result.Payload.Should().NotBeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetSecretFromSecretManager")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSecretFromSecretManager_WithCredentialFilePath_HandlesGracefully()
        {
            // Act — passing a credential path exercises the optional parameter code path
            var result = await _operations.GetSecretFromSecretManager(
                "my-project", "my-secret-id", credentialFilePath: "/path/to/key.json");

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null, "secret-id", "pass-id")]
        [InlineData("my-project", null, "pass-id")]
        public async Task GetCertificateFromSecretManager_WithNullParameters_HandlesGracefully(
            string? projectId, string? certSecretId, string? certPassId)
        {
            // Act
            var result = await _operations.GetCertificateFromSecretManager(
                projectId!, certSecretId!, certPassId!);

            // Assert
            result.Should().BeNull();
        }
    }
}
