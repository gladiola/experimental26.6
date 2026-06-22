using System.Security.Claims;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    public class AdminCertificateSettingsTests
    {
        [Fact]
        public void IsIssuerAllowed_ReturnsFalse_WhenAllowedIssuersEmpty()
        {
            var settings = new AdminCertificateSettings();

            settings.IsIssuerAllowed("CN=Admin CA").Should().BeFalse();
        }

        [Fact]
        public void FindAuthorizedAdmin_ReturnsMapping_WhenUserAndThumbprintMatch()
        {
            var settings = new AdminCertificateSettings
            {
                AllowedIssuers = new List<string> { "Admin CA" },
                AdminUsers =
                {
                    new AdminCertificateUserMapping
                    {
                        DisplayName = "Alice",
                        UserIdentifiers = new List<string> { "admin@example.com" },
                        AllowedThumbprints = new List<string> { "AA:BB:CC" }
                    }
                }
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Email, "admin@example.com") },
                "Test"));

            var result = settings.FindAuthorizedAdmin(user, "aabbcc");

            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("Alice");
        }
    }
}
