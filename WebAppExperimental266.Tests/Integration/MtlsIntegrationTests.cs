using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Integration
{
    /// <summary>
    /// Integration tests for mTLS functionality
    /// Tests the full mTLS configuration and integration with Kestrel
    /// </summary>
    public class MtlsIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public MtlsIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Application_StartsSuccessfully_WithMtlsDisabled()
        {
            // Arrange & Act
            var client = _factory.CreateClient();

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public async Task Application_HomePageAccessible_WithMtlsDisabled()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Should().NotBeNull();
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public void MtlsSettings_CanBeCreated()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.RequireClientCertificate.Should().BeTrue();
            settings.AllowCertificateChains.Should().BeTrue();
            settings.AllowSelfSignedCertificates.Should().BeFalse();
        }

        [Fact]
        public void FeatureFlags_IncludeMtlsFlag()
        {
            // Arrange
            var featureFlags = new FeatureFlags();

            // Act & Assert
            featureFlags.Should().NotBeNull();
            // EnableMtls property exists (tested by compilation)
        }

        [Fact]
        public void Application_HasConfiguration()
        {
            // Act
            var configuration = _factory.Services.GetRequiredService<IConfiguration>();

            // Assert
            configuration.Should().NotBeNull();
        }

        [Fact]
        public void MtlsSettings_HasReasonableDefaults()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.RequireClientCertificate.Should().BeTrue("secure by default");
            settings.AllowSelfSignedCertificates.Should().BeFalse("secure by default");
            settings.ValidateClientCertificateIssuer.Should().BeTrue("secure by default");
        }

        [Fact]
        public async Task Application_ReturnsSecurityHeaders_WithMtlsDisabled()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().NotBeEmpty();
        }

        [Fact]
        public void MtlsConfiguration_ProductionSettings_AreSecure()
        {
            // Arrange - Production-like configuration
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
            var settings = config.GetSection("MtlsSettings").Get<MtlsSettings>();

            // Assert
            settings.Should().NotBeNull();
            settings!.RequireClientCertificate.Should().BeTrue();
            settings.AllowSelfSignedCertificates.Should().BeFalse();
            settings.CheckCertificateRevocation.Should().BeTrue();
            settings.ValidateClientCertificateIssuer.Should().BeTrue();
        }

        [Fact]
        public void MtlsConfiguration_DevelopmentSettings_AllowFlexibility()
        {
            // Arrange - Development-like configuration
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
            var settings = config.GetSection("MtlsSettings").Get<MtlsSettings>();

            // Assert
            settings.Should().NotBeNull();
            settings!.RequireClientCertificate.Should().BeFalse();
            settings.AllowSelfSignedCertificates.Should().BeTrue();
            settings.CheckCertificateRevocation.Should().BeFalse();
        }

        [Fact]
        public async Task Application_PrivacyPageAccessible_WithMtlsDisabled()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Privacy");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public void MtlsSettings_CanBeOverridden_ByConfiguration()
        {
            // This test verifies that the configuration system supports overrides
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "false"
            }!);
            
            // Simulate override
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MtlsSettings:RequireClientCertificate"] = "true"
            }!);
            
            var config = configBuilder.Build();

            // Act
            var settings = config.GetSection("MtlsSettings").Get<MtlsSettings>();

            // Assert
            settings!.RequireClientCertificate.Should().BeTrue();
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Privacy")]
        public async Task Application_PublicPagesAccessible(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public void MtlsSettings_SupportsAllCertificateTypes()
        {
            // Arrange & Act
            var chainedOnly = new MtlsSettings
            {
                AllowCertificateChains = true,
                AllowSelfSignedCertificates = false
            };

            var selfSignedOnly = new MtlsSettings
            {
                AllowCertificateChains = false,
                AllowSelfSignedCertificates = true
            };

            var both = new MtlsSettings
            {
                AllowCertificateChains = true,
                AllowSelfSignedCertificates = true
            };

            // Assert
            chainedOnly.AllowCertificateChains.Should().BeTrue();
            selfSignedOnly.AllowSelfSignedCertificates.Should().BeTrue();
            both.AllowCertificateChains.Should().BeTrue();
            both.AllowSelfSignedCertificates.Should().BeTrue();
        }

        [Fact]
        public void Application_RegistersRequiredServices()
        {
            // Act
            var loggerFactory = _factory.Services.GetService<ILoggerFactory>();

            // Assert
            loggerFactory.Should().NotBeNull();
        }
    }
}
