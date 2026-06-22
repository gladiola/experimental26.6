using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AuthenticatedUserRequirementHandlerTests
    {
        [Fact]
        public async Task HandleAsync_Succeeds_ForStandardAuthenticatedUser()
        {
            var user = BuildUser(("oid", "user-1"), (ClaimTypes.Email, "user@example.com"));
            var context = BuildContext(user);
            var handler = BuildHandler(new AdminCertificateSettings(), null);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_Fails_ForMappedAdminCertificateUser()
        {
            var certificate = CreateSelfSignedCertificate("CN=Admin CA");
            var settings = new AdminCertificateSettings
            {
                AllowedIssuers = new List<string> { "CN=Admin CA" },
                AdminUsers =
                {
                    new AdminCertificateUserMapping
                    {
                        DisplayName = "Admin",
                        UserIdentifiers = new List<string> { "admin@example.com" },
                        AllowedThumbprints = new List<string> { certificate.Thumbprint! }
                    }
                }
            };

            var user = BuildUser(("oid", "admin-id"), (ClaimTypes.Email, "admin@example.com"));
            var context = BuildContext(user);
            var handler = BuildHandler(settings, certificate);

            await handler.HandleAsync(context);

            context.HasFailed.Should().BeTrue();
        }

        private static AuthorizationHandlerContext BuildContext(ClaimsPrincipal user)
        {
            return new AuthorizationHandlerContext(
                new[] { new AuthenticatedUserRequirement() },
                user,
                resource: null);
        }

        private static AuthenticatedUserRequirementHandler BuildHandler(
            AdminCertificateSettings settings,
            X509Certificate2? certificate)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.ClientCertificate = certificate;
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            return new AuthenticatedUserRequirementHandler(accessor, settings);
        }

        private static ClaimsPrincipal BuildUser(params (string Type, string Value)[] claims)
        {
            var identity = new ClaimsIdentity(
                claims.Select(claim => new Claim(claim.Type, claim.Value)),
                "Test");

            return new ClaimsPrincipal(identity);
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string distinguishedName)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(
                distinguishedName,
                key,
                HashAlgorithmName.SHA256);

            return request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddHours(-1),
                DateTimeOffset.UtcNow.AddDays(30));
        }
    }
}
