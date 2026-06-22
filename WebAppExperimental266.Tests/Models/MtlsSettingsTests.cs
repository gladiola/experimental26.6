using FluentAssertions;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    /// <summary>
    /// Unit tests for MtlsSettings configuration model
    /// </summary>
    public class MtlsSettingsTests
    {
        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.RequireClientCertificate.Should().BeTrue("client certificates should be required by default for security");
            settings.AllowCertificateChains.Should().BeTrue("chained certificates should be allowed by default");
            settings.AllowSelfSignedCertificates.Should().BeFalse("self-signed certificates should not be allowed by default");
            settings.CheckCertificateRevocation.Should().BeTrue("revocation check must be enabled by default for security (fix #6)");
            settings.ValidateClientCertificateIssuer.Should().BeTrue("issuer validation should be enabled by default");
            settings.ClientCertificateName.Should().BeNull("certificate name is optional");
        }

        [Fact]
        public void AllProperties_CanBeSet()
        {
            // Act
            var settings = new MtlsSettings
            {
                RequireClientCertificate = false,
                AllowCertificateChains = false,
                AllowSelfSignedCertificates = true,
                CheckCertificateRevocation = true,
                ClientCertificateName = "my-client-cert",
                ValidateClientCertificateIssuer = false
            };

            // Assert
            settings.RequireClientCertificate.Should().BeFalse();
            settings.AllowCertificateChains.Should().BeFalse();
            settings.AllowSelfSignedCertificates.Should().BeTrue();
            settings.CheckCertificateRevocation.Should().BeTrue();
            settings.ClientCertificateName.Should().Be("my-client-cert");
            settings.ValidateClientCertificateIssuer.Should().BeFalse();
        }

        [Fact]
        public void RequireClientCertificate_DefaultsToTrue()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.RequireClientCertificate.Should().BeTrue();
        }

        [Fact]
        public void AllowSelfSignedCertificates_DefaultsToFalse()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.AllowSelfSignedCertificates.Should().BeFalse();
        }

        [Fact]
        public void AllowCertificateChains_DefaultsToTrue()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.AllowCertificateChains.Should().BeTrue();
        }

        [Fact]
        public void CheckCertificateRevocation_DefaultsToTrue()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert — revocation must be on by default (security fix #6)
            settings.CheckCertificateRevocation.Should().BeTrue(
                "revocation checking must default to true so that revoked certificates cannot authenticate");
        }

        [Fact]
        public void ValidateClientCertificateIssuer_DefaultsToTrue()
        {
            // Act
            var settings = new MtlsSettings();

            // Assert
            settings.ValidateClientCertificateIssuer.Should().BeTrue();
        }

        [Fact]
        public void ClientCertificateName_CanBeNull()
        {
            // Act
            var settings = new MtlsSettings
            {
                ClientCertificateName = null
            };

            // Assert
            settings.ClientCertificateName.Should().BeNull();
        }

        [Fact]
        public void ClientCertificateName_CanBeEmptyString()
        {
            // Act
            var settings = new MtlsSettings
            {
                ClientCertificateName = string.Empty
            };

            // Assert
            settings.ClientCertificateName.Should().BeEmpty();
        }

        [Theory]
        [InlineData("client-cert-prod")]
        [InlineData("dev-certificate")]
        [InlineData("my-app-client-cert-2024")]
        public void ClientCertificateName_AcceptsValidNames(string certName)
        {
            // Act
            var settings = new MtlsSettings
            {
                ClientCertificateName = certName
            };

            // Assert
            settings.ClientCertificateName.Should().Be(certName);
        }

        [Fact]
        public void ProductionConfiguration_HasSecureDefaults()
        {
            // Arrange - Production scenario
            var settings = new MtlsSettings
            {
                RequireClientCertificate = true,
                AllowCertificateChains = true,
                AllowSelfSignedCertificates = false,
                CheckCertificateRevocation = true,
                ValidateClientCertificateIssuer = true
            };

            // Assert
            settings.RequireClientCertificate.Should().BeTrue("production should require client certificates");
            settings.AllowSelfSignedCertificates.Should().BeFalse("production should not allow self-signed certificates");
            settings.CheckCertificateRevocation.Should().BeTrue("production should check certificate revocation");
            settings.ValidateClientCertificateIssuer.Should().BeTrue("production should validate certificate issuer");
        }

        [Fact]
        public void DevelopmentConfiguration_AllowsFlexibility()
        {
            // Arrange - Development scenario
            var settings = new MtlsSettings
            {
                RequireClientCertificate = false,
                AllowSelfSignedCertificates = true,
                CheckCertificateRevocation = false,
                ValidateClientCertificateIssuer = false
            };

            // Assert
            settings.RequireClientCertificate.Should().BeFalse("development can make certificates optional");
            settings.AllowSelfSignedCertificates.Should().BeTrue("development can allow self-signed certificates");
            settings.CheckCertificateRevocation.Should().BeFalse("development typically skips revocation checks");
            settings.ValidateClientCertificateIssuer.Should().BeFalse("development may skip issuer validation");
        }

        [Fact]
        public void MultipleInstances_AreIndependent()
        {
            // Arrange
            var settings1 = new MtlsSettings { RequireClientCertificate = true };
            var settings2 = new MtlsSettings { RequireClientCertificate = false };

            // Assert
            settings1.RequireClientCertificate.Should().BeTrue();
            settings2.RequireClientCertificate.Should().BeFalse();
            settings1.Should().NotBeSameAs(settings2);
        }

        [Fact]
        public void BothCertificateTypesEnabled_IsValid()
        {
            // Arrange - Allow both chained and self-signed (dev scenario)
            var settings = new MtlsSettings
            {
                AllowCertificateChains = true,
                AllowSelfSignedCertificates = true
            };

            // Assert
            settings.AllowCertificateChains.Should().BeTrue();
            settings.AllowSelfSignedCertificates.Should().BeTrue();
        }

        [Fact]
        public void BothCertificateTypesDisabled_IsValid()
        {
            // Arrange - Edge case, though not recommended
            var settings = new MtlsSettings
            {
                AllowCertificateChains = false,
                AllowSelfSignedCertificates = false
            };

            // Assert
            settings.AllowCertificateChains.Should().BeFalse();
            settings.AllowSelfSignedCertificates.Should().BeFalse();
        }

        [Fact]
        public void RevocationCheckEnabled_WithoutRequiringCertificate_IsValid()
        {
            // Arrange - Certificate is optional but if provided, check revocation
            var settings = new MtlsSettings
            {
                RequireClientCertificate = false,
                CheckCertificateRevocation = true
            };

            // Assert
            settings.RequireClientCertificate.Should().BeFalse();
            settings.CheckCertificateRevocation.Should().BeTrue();
        }

        [Fact]
        public void SettingsObject_IsSerializable()
        {
            // Arrange
            var settings = new MtlsSettings
            {
                RequireClientCertificate = true,
                ClientCertificateName = "test-cert"
            };

            // Act & Assert - If this doesn't throw, the object is serializable
            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("RequireClientCertificate");
            json.Should().Contain("test-cert");
        }

        [Fact]
        public void SettingsObject_IsDeserializable()
        {
            // Arrange
            var json = @"{
                ""RequireClientCertificate"": false,
                ""AllowCertificateChains"": false,
                ""AllowSelfSignedCertificates"": true,
                ""CheckCertificateRevocation"": true,
                ""ClientCertificateName"": ""deserialized-cert"",
                ""ValidateClientCertificateIssuer"": false
            }";

            // Act
            var settings = System.Text.Json.JsonSerializer.Deserialize<MtlsSettings>(json);

            // Assert
            settings.Should().NotBeNull();
            settings!.RequireClientCertificate.Should().BeFalse();
            settings.AllowSelfSignedCertificates.Should().BeTrue();
            settings.ClientCertificateName.Should().Be("deserialized-cert");
        }

        // ── Security fix #5: issuer validation ─────────────────────────────────

        [Fact]
        public void AllowedIssuers_DefaultsToEmptyList()
        {
            var settings = new MtlsSettings();
            settings.AllowedIssuers.Should().NotBeNull();
            settings.AllowedIssuers.Should().BeEmpty();
        }

        [Fact]
        public void IsIssuerAllowed_ReturnsFalse_WhenValidationEnabled_AndAllowedIssuersEmpty()
        {
            // If ValidateClientCertificateIssuer = true but no issuers configured,
            // the method must reject every issuer (fail-closed).
            var settings = new MtlsSettings
            {
                ValidateClientCertificateIssuer = true,
                AllowedIssuers = new List<string>()
            };

            settings.IsIssuerAllowed("CN=Any CA").Should().BeFalse(
                "an empty allowed-issuers list should deny all issuers (security fix #5)");
        }

        [Fact]
        public void IsIssuerAllowed_ReturnsTrue_WhenValidationDisabled()
        {
            var settings = new MtlsSettings
            {
                ValidateClientCertificateIssuer = false,
                AllowedIssuers = new List<string>()
            };

            settings.IsIssuerAllowed("CN=Any CA").Should().BeTrue(
                "when issuer validation is disabled, all issuers should be accepted");
        }

        [Theory]
        [InlineData("CN=My Company CA, O=MyOrg", "My Company CA", true)]
        [InlineData("CN=My Company CA, O=MyOrg", "MYORG", true)]
        [InlineData("CN=My Company CA, O=MyOrg", "Unknown CA", false)]
        [InlineData("CN=My Company CA, O=MyOrg", "evil.com", false)]
        public void IsIssuerAllowed_MatchesSubstring_CaseInsensitive(
            string issuer, string allowedEntry, bool expectedResult)
        {
            var settings = new MtlsSettings
            {
                ValidateClientCertificateIssuer = true,
                AllowedIssuers = new List<string> { allowedEntry }
            };

            settings.IsIssuerAllowed(issuer).Should().Be(expectedResult,
                "IsIssuerAllowed must perform case-insensitive substring matching (security fix #5)");
        }

        [Fact]
        public void IsIssuerAllowed_ReturnsFalse_ForUntrustedIssuer_WhenAllowedIssuersConfigured()
        {
            // This is the regression test: if the validation is removed/commented out,
            // any issuer would be accepted and this test would fail.
            var settings = new MtlsSettings
            {
                ValidateClientCertificateIssuer = true,
                AllowedIssuers = new List<string> { "CN=Trusted CA" }
            };

            settings.IsIssuerAllowed("CN=Untrusted CA, O=Attacker").Should().BeFalse(
                "certificates from issuers not in AllowedIssuers must be rejected (security fix #5)");
            settings.IsIssuerAllowed("CN=Trusted CA, O=MyOrg").Should().BeTrue(
                "certificates from an allowed issuer must be accepted");
        }
    }
}
