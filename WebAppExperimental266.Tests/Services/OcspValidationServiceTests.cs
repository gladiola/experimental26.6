using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Unit tests for OCSP validation service
    /// </summary>
    public class OcspValidationServiceTests
    {
        private readonly Mock<ILogger<OcspValidationService>> _mockLogger;
        private readonly OcspSettings _settings;
        private readonly OcspValidationService _service;

        public OcspValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<OcspValidationService>>();
            
            _settings = new OcspSettings
            {
                EnableOcspValidation = true,
                OcspServerUrl = "https://ocsp.test.com",
                RequestTimeoutSeconds = 30,
                MaxRetryAttempts = 3,
                CacheDurationMinutes = 60,
                ServerUnavailableBehavior = "Fail"
            };

            _service = new OcspValidationService(_mockLogger.Object, _settings, new HttpClient());
        }

        [Fact]
        public void Constructor_AcceptsAllDependencies()
        {
            // Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public async Task ValidateCertificateAsync_WhenDisabled_ReturnsTrue()
        {
            // Arrange
            _settings.EnableOcspValidation = false;
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateAsync(cert);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WhenDisabled_ReturnsDisabledStatus()
        {
            // Arrange
            _settings.EnableOcspValidation = false;
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be(OcspStatus.Disabled);
            result.Message.Should().Contain("disabled");
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WhenNoServerUrl_ReturnsServerUnavailable()
        {
            // Arrange
            _settings.OcspServerUrl = null;
            _settings.ServerUnavailableBehavior = "Warn";
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Status.Should().Be(OcspStatus.ServerUnavailable);
            result.Message.Should().Contain("not configured");
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WhenNoServerUrl_AndFailBehavior_ReturnsFalse()
        {
            // Arrange
            _settings.OcspServerUrl = null;
            _settings.ServerUnavailableBehavior = "Fail";
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Status.Should().Be(OcspStatus.ServerUnavailable);
        }

        /// <summary>
        /// Security fix #7: when OCSP validation is enabled and the OCSP server URL is
        /// configured, the stub implementation must return IsValid = false rather than
        /// silently passing every certificate as valid.
        /// This test fails if the stub is reverted to always returning IsValid = true.
        /// </summary>
        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WhenEnabled_StubReturnsFalse_NotSilentlyValid()
        {
            // Arrange — OCSP is enabled and a server URL is present (normal production config)
            _settings.EnableOcspValidation = true;
            _settings.OcspServerUrl = "https://ocsp.test.com";
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert — the stub must reject the certificate (fail closed), never silently approve
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse(
                "the OCSP stub must fail closed so it cannot silently pass revoked certificates (security fix #7)");
            result.Status.Should().Be(OcspStatus.Error,
                "the stub should report an Error status to clearly indicate it is not implemented");
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_CachesResults()
        {
            // Arrange — disable OCSP so we get a cacheable result
            _settings.EnableOcspValidation = false;
            var cert = CreateTestCertificate();

            // Act - First call
            var result1 = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Act - Second call (should use cache)
            var result2 = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert - Both calls should return same result
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Status.Should().Be(result2.Status);
        }

        [Theory]
        [InlineData("Fail")]
        [InlineData("Allow")]
        [InlineData("Warn")]
        public async Task ValidateCertificateWithDetailsAsync_ServerUnavailableBehavior_FailsClosed(
            string behavior)
        {
            // Arrange
            _settings.OcspServerUrl = null;
            _settings.ServerUnavailableBehavior = behavior;
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Status.Should().Be(OcspStatus.ServerUnavailable);
        }

        [Fact]
        public async Task ValidateCertificateAsync_ReturnsBooleanResult()
        {
            // Arrange — disable OCSP so we get a predictable result
            _settings.EnableOcspValidation = false;
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateAsync(cert);

            // Assert
            result.Should().BeTrue(); // disabled OCSP returns true
        }

        [Fact]
        public void OcspValidationResult_HasAllRequiredProperties()
        {
            // Act
            var result = new OcspValidationResult
            {
                IsValid = true,
                Status = OcspStatus.Good,
                Message = "Test message",
                ValidatedAt = DateTime.UtcNow,
                CertificateThumbprint = "ABC123",
                CertificateSubject = "CN=Test"
            };

            // Assert
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be(OcspStatus.Good);
            result.Message.Should().Be("Test message");
            result.ValidatedAt.Should().NotBeNull();
            result.CertificateThumbprint.Should().Be("ABC123");
            result.CertificateSubject.Should().Be("CN=Test");
        }

        [Theory]
        [InlineData(OcspStatus.Good)]
        [InlineData(OcspStatus.Revoked)]
        [InlineData(OcspStatus.Unknown)]
        [InlineData(OcspStatus.Disabled)]
        [InlineData(OcspStatus.ServerUnavailable)]
        [InlineData(OcspStatus.Warning)]
        [InlineData(OcspStatus.Error)]
        public void OcspStatus_AllValuesAreValid(OcspStatus status)
        {
            // Act
            var result = new OcspValidationResult
            {
                Status = status
            };

            // Assert
            result.Status.Should().Be(status);
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WithDetailedLogging_LogsInformation()
        {
            // Arrange
            _settings.EnableDetailedLogging = true;
            _settings.EnableOcspValidation = false; // use disabled path for predictable logging
            var cert = CreateTestCertificate();

            // Act
            await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateCertificateWithDetailsAsync_WhenEnabled_PopulatesCertificateInformation()
        {
            // Arrange — use the stub (OCSP enabled)
            _settings.EnableOcspValidation = true;
            _settings.OcspServerUrl = "https://ocsp.test.com";
            var cert = CreateTestCertificate();

            // Act
            var result = await _service.ValidateCertificateWithDetailsAsync(cert);

            // Assert — the stub must still populate certificate fields even on failure
            result.CertificateThumbprint.Should().Be(cert.Thumbprint);
            result.CertificateSubject.Should().Be(cert.Subject);
        }

        /// <summary>
        /// Helper method to create a test certificate
        /// </summary>
        private X509Certificate2 CreateTestCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                new X500DistinguishedName("CN=Test Certificate"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(365));
        }
    }
}
