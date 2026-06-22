using System.Security.Claims;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class UserIdentityHelperTests
    {
        [Fact]
        public void GetStableUserId_UsesOid_WhenPresent()
        {
            var principal = BuildPrincipal(
                isAuthenticated: true,
                ("oid", "stable-oid"),
                (ClaimTypes.Email, "user@example.com"));

            var stableId = UserIdentityHelper.GetStableUserId(principal);

            stableId.Should().Be("stable-oid");
        }

        [Fact]
        public void GetStableUserId_UsesSub_WhenOidMissing()
        {
            var principal = BuildPrincipal(
                isAuthenticated: true,
                ("sub", "stable-sub"),
                (ClaimTypes.Email, "user@example.com"));

            var stableId = UserIdentityHelper.GetStableUserId(principal);

            stableId.Should().Be("stable-sub");
        }

        [Fact]
        public void GetStableUserId_Throws_WhenAuthenticatedUserHasNoStableClaim()
        {
            var principal = BuildPrincipal(
                isAuthenticated: true,
                (ClaimTypes.Email, "user@example.com"),
                ("preferred_username", "user"));

            var action = () => UserIdentityHelper.GetStableUserId(principal);

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetStableUserId_ReturnsAnonymous_ForUnauthenticatedUser()
        {
            var principal = BuildPrincipal(isAuthenticated: false, (ClaimTypes.Email, "guest@example.com"));

            var stableId = UserIdentityHelper.GetStableUserId(principal);

            stableId.Should().Be("anonymous");
        }

        private static ClaimsPrincipal BuildPrincipal(bool isAuthenticated, params (string Type, string Value)[] claims)
        {
            var identity = new ClaimsIdentity(
                claims.Select(claim => new Claim(claim.Type, claim.Value)),
                isAuthenticated ? "Test" : null);

            return new ClaimsPrincipal(identity);
        }
    }
}
