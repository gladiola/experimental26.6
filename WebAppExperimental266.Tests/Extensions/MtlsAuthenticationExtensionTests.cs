using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Extensions;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Extensions
{
    /// <summary>
    /// Unit tests for mTLS authentication extension methods
    /// </summary>
    public class MtlsAuthenticationExtensionTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public MtlsAuthenticationExtensionTests()
        {
            _mockLogger = new Mock<ILogger>();
            _services = new ServiceCollection();
            
            // Setup configuration with mTLS settings
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "false",
                ["MtlsSettings:CheckCertificateRevocation"] = "false",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true",
                ["MtlsSettings:ClientCertificateName"] = "test-cert"
            }!);
            _configuration = configBuilder.Build();
        }

        [Fact]
        public void AddMtlsAuthentication_RegistersServices_WhenEnabled()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var authenticationSchemeProvider = serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            
            // Service collection should have authentication services
            _services.Should().NotBeEmpty();
        }

        [Fact]
        public void AddMtlsAuthentication_DoesNotRegisterServices_WhenDisabled()
        {
            // Arrange
            var initialCount = _services.Count;

            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: false);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("mTLS Client Certificate Authentication is DISABLED")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _services.Count.Should().Be(initialCount, "no services should be added when disabled");
        }

        [Fact]
        public void AddMtlsAuthentication_ThrowsException_WhenMtlsSettingsNotFound()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();

            // Act & Assert
            var action = () => _services.AddMtlsAuthentication(emptyConfig, _mockLogger.Object, enabled: true);
            
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*MtlsSettings not found*");
        }

        [Fact]
        public void AddMtlsAuthentication_LogsCorrectly_WhenEnabled()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuring mTLS certificate authentication")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void AddMtlsAuthentication_LogsCorrectly_WhenDisabled()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: false);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DISABLED")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(true, true, "both chained AND self-signed")]
        [InlineData(true, false, "chained certificates only")]
        [InlineData(false, true, "self-signed certificates only")]
        public void AddMtlsAuthentication_ConfiguresCertificateTypes_Correctly(
            bool allowChained, 
            bool allowSelfSigned, 
            string expectedLogMessage)
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = allowChained.ToString(),
                ["MtlsSettings:AllowSelfSignedCertificates"] = allowSelfSigned.ToString(),
                ["MtlsSettings:CheckCertificateRevocation"] = "false",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true"
            }!);
            var config = configBuilder.Build();

            // Act
            _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLogMessage)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(true, "ENABLED")]
        [InlineData(false, "DISABLED")]
        public void AddMtlsAuthentication_ConfiguresRevocationCheck_Correctly(
            bool checkRevocation,
            string expectedStatus)
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "false",
                ["MtlsSettings:CheckCertificateRevocation"] = checkRevocation.ToString(),
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true"
            }!);
            var config = configBuilder.Build();

            // Act
            _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Certificate revocation check = {expectedStatus}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void AddMtlsAuthentication_LogsSuccessMessage_AfterConfiguration()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("mTLS certificate authentication configured successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void AddMtlsAuthentication_ReturnsServiceCollection()
        {
            // Act
            var result = _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert
            result.Should().BeSameAs(_services);
        }

        [Fact]
        public void AddMtlsAuthentication_WithMinimalConfiguration_WorksCorrectly()
        {
            // Arrange - Minimal configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "false",
                ["MtlsSettings:CheckCertificateRevocation"] = "false",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true"
            }!);
            var config = configBuilder.Build();

            // Act
            var action = () => _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void AddMtlsAuthentication_WithOptionalCertificateName_WorksCorrectly()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "false",
                ["MtlsSettings:CheckCertificateRevocation"] = "false",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true",
                ["MtlsSettings:ClientCertificateName"] = "my-cert-name"
            }!);
            var config = configBuilder.Build();

            // Act
            var action = () => _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void AddMtlsAuthentication_ConfiguresAuthenticationScheme()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var schemes = serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            schemes.Should().NotBeNull();
        }

        [Fact]
        public void AddMtlsAuthentication_CanBeCalledMultipleTimes()
        {
            // Act
            _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);
            var action = () => _services.AddMtlsAuthentication(_configuration, _mockLogger.Object, enabled: true);

            // Assert - Should not throw, though it may override previous configuration
            action.Should().NotThrow();
        }

        [Fact]
        public void AddMtlsAuthentication_WithProductionSettings_LogsSecureConfiguration()
        {
            // Arrange - Production-like settings
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "false",
                ["MtlsSettings:CheckCertificateRevocation"] = "true",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "true"
            }!);
            var config = configBuilder.Build();

            // Act
            _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("chained certificates only")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void AddMtlsAuthentication_WithDevelopmentSettings_LogsWarnings()
        {
            // Arrange - Development-like settings (less secure)
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "false",
                ["MtlsSettings:AllowCertificateChains"] = "true",
                ["MtlsSettings:AllowSelfSignedCertificates"] = "true",
                ["MtlsSettings:CheckCertificateRevocation"] = "false",
                ["MtlsSettings:ValidateClientCertificateIssuer"] = "false"
            }!);
            var config = configBuilder.Build();

            // Act
            _services.AddMtlsAuthentication(config, _mockLogger.Object, enabled: true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("both chained AND self-signed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
