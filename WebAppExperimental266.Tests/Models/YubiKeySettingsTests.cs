using FluentAssertions;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    /// <summary>
    /// Unit tests for YubiKeySettings configuration model.
    /// </summary>
    public class YubiKeySettingsTests
    {
        // ── Default values ──────────────────────────────────────────────────────

        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            var settings = new YubiKeySettings();

            settings.AllowedCaIssuers.Should().NotBeNull().And.BeEmpty();
            settings.AdminCaThumbprints.Should().NotBeNull().And.BeEmpty();
            settings.RequireYubiKeyAttestation.Should().BeFalse();
            settings.YubiKeyAttestationIssuer.Should().BeNull();
            settings.SkipOcspInDevelopment.Should().BeTrue();
        }

        [Fact]
        public void AllProperties_CanBeSet()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "CN=OpenBSD Admin CA, O=MyOrg" },
                AdminCaThumbprints = new List<string> { "AABBCCDDEEFF0011" },
                RequireYubiKeyAttestation = true,
                YubiKeyAttestationIssuer = "Yubico PIV Attestation",
                SkipOcspInDevelopment = false
            };

            settings.AllowedCaIssuers.Should().ContainSingle("CN=OpenBSD Admin CA, O=MyOrg");
            settings.AdminCaThumbprints.Should().ContainSingle("AABBCCDDEEFF0011");
            settings.RequireYubiKeyAttestation.Should().BeTrue();
            settings.YubiKeyAttestationIssuer.Should().Be("Yubico PIV Attestation");
            settings.SkipOcspInDevelopment.Should().BeFalse();
        }

        // ── IsIssuerAllowed ─────────────────────────────────────────────────────

        [Fact]
        public void IsIssuerAllowed_ReturnsFalse_WhenAllowedCaIssuersIsEmpty()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string>()
            };

            settings.IsIssuerAllowed("CN=Any CA").Should().BeFalse(
                "an empty AllowedCaIssuers list must fail closed for YubiKey MFA");
        }

        [Fact]
        public void IsIssuerAllowed_ReturnsTrue_WhenIssuerMatchesAllowedEntry()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "OpenBSD Admin CA" }
            };

            settings.IsIssuerAllowed("CN=OpenBSD Admin CA, O=MyOrg").Should().BeTrue();
        }

        [Fact]
        public void IsIssuerAllowed_ReturnsFalse_WhenIssuerNotInList()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "CN=OpenBSD Admin CA" }
            };

            settings.IsIssuerAllowed("CN=Rogue CA, O=Attacker").Should().BeFalse();
        }

        [Theory]
        [InlineData("CN=OpenBSD Admin CA, O=MyOrg", "OpenBSD Admin CA", true)]
        [InlineData("CN=OpenBSD Admin CA, O=MyOrg", "OPENBSD ADMIN CA", true)]   // case insensitive
        [InlineData("CN=OpenBSD Admin CA, O=MyOrg", "MyOrg", true)]
        [InlineData("CN=OpenBSD Admin CA, O=MyOrg", "Evil Corp", false)]
        public void IsIssuerAllowed_MatchesSubstring_CaseInsensitive(
            string issuer, string allowedEntry, bool expected)
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { allowedEntry }
            };

            settings.IsIssuerAllowed(issuer).Should().Be(expected);
        }

        [Fact]
        public void IsIssuerAllowed_AcceptsWhenAnyEntryMatches()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "CN=Staging CA", "CN=OpenBSD Admin CA" }
            };

            settings.IsIssuerAllowed("CN=OpenBSD Admin CA, O=MyOrg").Should().BeTrue();
        }

        // ── IsThumbprintAllowed ─────────────────────────────────────────────────

        [Fact]
        public void IsThumbprintAllowed_ReturnsTrueForAll_WhenListIsEmpty()
        {
            var settings = new YubiKeySettings
            {
                AdminCaThumbprints = new List<string>()
            };

            settings.IsThumbprintAllowed("AABBCC001122").Should().BeTrue(
                "when AdminCaThumbprints is empty there is no restriction");
        }

        [Fact]
        public void IsThumbprintAllowed_ReturnsTrue_WhenThumbprintMatches()
        {
            var settings = new YubiKeySettings
            {
                AdminCaThumbprints = new List<string> { "AABBCCDDEEFF0011" }
            };

            settings.IsThumbprintAllowed("AABBCCDDEEFF0011").Should().BeTrue();
        }

        [Fact]
        public void IsThumbprintAllowed_ReturnsFalse_WhenThumbprintNotInList()
        {
            var settings = new YubiKeySettings
            {
                AdminCaThumbprints = new List<string> { "AABBCCDDEEFF0011" }
            };

            settings.IsThumbprintAllowed("112233445566AABB").Should().BeFalse();
        }

        [Theory]
        [InlineData("AABBCCDDEEFF", "aabbccddeeff", true)]   // case insensitive
        [InlineData("AA:BB:CC:DD", "AABBCCDD", true)]         // colon separators stripped
        [InlineData("AA BB CC DD", "AABBCCDD", true)]         // space separators stripped
        [InlineData("AABBCC", "001122", false)]
        public void IsThumbprintAllowed_NormalizesInput(
            string stored, string tested, bool expected)
        {
            var settings = new YubiKeySettings
            {
                AdminCaThumbprints = new List<string> { stored }
            };

            settings.IsThumbprintAllowed(tested).Should().Be(expected);
        }

        // ── Serialization ───────────────────────────────────────────────────────

        [Fact]
        public void SettingsObject_IsSerializable()
        {
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "CN=Test CA" },
                RequireYubiKeyAttestation = true,
                YubiKeyAttestationIssuer = "Yubico PIV Attestation"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("CN=Test CA");
            json.Should().Contain("YubiKeyAttestationIssuer");
        }

        [Fact]
        public void SettingsObject_IsDeserializable()
        {
            var json = @"{
                ""AllowedCaIssuers"": [""CN=OpenBSD Admin CA""],
                ""AdminCaThumbprints"": [""AABB1122""],
                ""RequireYubiKeyAttestation"": true,
                ""YubiKeyAttestationIssuer"": ""Yubico PIV Attestation"",
                ""SkipOcspInDevelopment"": false
            }";

            var settings = System.Text.Json.JsonSerializer.Deserialize<YubiKeySettings>(json);

            settings.Should().NotBeNull();
            settings!.AllowedCaIssuers.Should().ContainSingle("CN=OpenBSD Admin CA");
            settings.AdminCaThumbprints.Should().ContainSingle("AABB1122");
            settings.RequireYubiKeyAttestation.Should().BeTrue();
            settings.YubiKeyAttestationIssuer.Should().Be("Yubico PIV Attestation");
            settings.SkipOcspInDevelopment.Should().BeFalse();
        }
    }
}
