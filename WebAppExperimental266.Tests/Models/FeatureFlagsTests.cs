using FluentAssertions;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    /// <summary>
    /// Unit tests for FeatureFlags configuration model — security fix #8.
    /// </summary>
    public class FeatureFlagsTests
    {
        /// <summary>
        /// Security fix #8: authentication and authorization must default to enabled so that a
        /// developer who creates a new appsettings file without explicitly setting these flags
        /// does not accidentally deploy an open application.
        /// This test fails if either default is changed back to false.
        /// </summary>
        [Fact]
        public void DefaultFlags_HaveAuthenticationAndAuthorizationEnabled()
        {
            // Act
            var flags = new FeatureFlags();

            // Assert
            flags.EnableAzureAd.Should().BeTrue(
                "authentication must be ON by default — opt-out is safer than opt-in (security fix #8)");
            flags.EnableAuthorization.Should().BeTrue(
                "authorization must be ON by default — opt-out is safer than opt-in (security fix #8)");
        }

        [Fact]
        public void DefaultFlags_EnableSessionAndLocalization()
        {
            var flags = new FeatureFlags();

            flags.EnableSession.Should().BeTrue();
            flags.EnableLocalization.Should().BeTrue();
        }

        [Fact]
        public void DefaultFlags_EnableSecurityHeaders()
        {
            var flags = new FeatureFlags();

            flags.EnableSecurityHeaders.Should().BeTrue();
        }

        [Fact]
        public void DefaultFlags_AwsCognito_IsDisabledByDefault()
        {
            var flags = new FeatureFlags();
            flags.EnableAwsCognito.Should().BeFalse(
                "AWS Cognito authentication is optional and must be explicitly opted into");
        }

        [Fact]
        public void DefaultFlags_GcpIdentity_IsDisabledByDefault()
        {
            var flags = new FeatureFlags();
            flags.EnableGcpIdentity.Should().BeFalse(
                "GCP Identity authentication is optional and must be explicitly opted into");
        }

        [Fact]
        public void DefaultFlags_AwsCognito_CanBeEnabled()
        {
            var flags = new FeatureFlags { EnableAwsCognito = true };
            flags.EnableAwsCognito.Should().BeTrue();
        }

        [Fact]
        public void DefaultFlags_GcpIdentity_CanBeEnabled()
        {
            var flags = new FeatureFlags { EnableGcpIdentity = true };
            flags.EnableGcpIdentity.Should().BeTrue();
        }

        [Fact]
        public void DefaultFlags_YubiKeyRequired_IsDisabledByDefault()
        {
            var flags = new FeatureFlags();
            flags.EnableYubiKeyRequired.Should().BeFalse(
                "YubiKey MFA is opt-in and must be explicitly enabled by an operator");
        }

        [Fact]
        public void DefaultFlags_YubiKeyRequired_CanBeEnabled()
        {
            var flags = new FeatureFlags { EnableYubiKeyRequired = true };
            flags.EnableYubiKeyRequired.Should().BeTrue();
        }

        [Fact]
        public void AllFlags_CanBeOverridden()
        {
            // Arrange — simulate an explicit opt-out (e.g. integration test environment)
            var flags = new FeatureFlags
            {
                EnableAzureAd = false,
                EnableAuthorization = false
            };

            // Assert — overrides still work when intentional
            flags.EnableAzureAd.Should().BeFalse();
            flags.EnableAuthorization.Should().BeFalse();
        }
    }
}
