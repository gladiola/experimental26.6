using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class GroupAdminCertificateRequirementHandlerTests
    {
        [Fact]
        public async Task HandleAsync_Succeeds_WhenGroupAdminMappingMatches()
        {
            var certificate = CreateSelfSignedCertificate("CN=GroupA Intermediate");
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
                                AllowedThumbprints = new List<string> { certificate.Thumbprint! }
                            }
                        }
                    }
                }
            };
            var user = BuildUser((ClaimTypes.Email, "group-admin@example.com"));
            var context = BuildContext(user);
            var handler = BuildHandler(settings, certificate);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        private static AuthorizationHandlerContext BuildContext(ClaimsPrincipal user)
            => new(new[] { new GroupAdminCertificateRequirement() }, user, resource: null);

        private static GroupAdminCertificateRequirementHandler BuildHandler(
            GroupAccessSettings settings,
            X509Certificate2 certificate)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.ClientCertificate = certificate;
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            return new GroupAdminCertificateRequirementHandler(
                accessor,
                settings,
                new NoOpAdminCertificateAuditService());
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

        private sealed class NoOpAdminCertificateAuditService : IAdminCertificateAuditService
        {
            public void LogAuthenticationAttempt(HttpContext? httpContext, ClaimsPrincipal user, bool success, string detail)
            {
            }

            public void LogPageAccess(HttpContext? httpContext, ClaimsPrincipal user, string pageName)
            {
            }
        }
    }
}
