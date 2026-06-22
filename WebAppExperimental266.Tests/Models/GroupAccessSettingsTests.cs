using System.Security.Claims;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    public class GroupAccessSettingsTests
    {
        [Fact]
        public void ResolveUserGroup_ReturnsMappedGroup_WhenUserAssignmentMatches()
        {
            var settings = new GroupAccessSettings
            {
                UserAssignments =
                {
                    new GroupUserAssignment
                    {
                        GroupId = "group-a",
                        UserIdentifiers = new List<string> { "user@example.com" }
                    }
                }
            };
            var user = BuildUser((ClaimTypes.Email, "user@example.com"));

            var groupId = settings.ResolveUserGroup(user, null);

            groupId.Should().Be("group-a");
        }

        [Fact]
        public void FindAuthorizedGroupAdmin_ReturnsAuthorization_WhenMappingMatches()
        {
            var settings = new GroupAccessSettings
            {
                Groups =
                {
                    new GroupDefinition
                    {
                        Id = "group-a",
                        DisplayName = "Group A",
                        CertificateIssuerFragment = "CN=GroupA Intermediate",
                        GroupAdmins =
                        {
                            new GroupAdminUserMapping
                            {
                                DisplayName = "Group Admin",
                                UserIdentifiers = new List<string> { "group-admin@example.com" },
                                AllowedThumbprints = new List<string> { "AA:BB:CC" }
                            }
                        }
                    }
                }
            };
            var user = BuildUser((ClaimTypes.Email, "group-admin@example.com"));

            var authorization = settings.FindAuthorizedGroupAdmin(
                user,
                "aabbcc",
                "CN=GroupA Intermediate, O=Example");

            authorization.Should().NotBeNull();
            authorization!.GroupId.Should().Be("group-a");
        }

        private static ClaimsPrincipal BuildUser(params (string Type, string Value)[] claims)
        {
            var identity = new ClaimsIdentity(
                claims.Select(claim => new Claim(claim.Type, claim.Value)),
                "Test");
            return new ClaimsPrincipal(identity);
        }
    }
}
