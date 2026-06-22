using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AdminCertificateRequirementHandlerTests
    {
        private readonly Mock<IAdminCertificateAuditService> _auditService = new();
        private readonly Mock<ILogger<AdminCertificateRequirementHandler>> _logger = new();

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenNoCertificatePresent()
        {
            var context = BuildAuthorizationContext(null, "admin@example.com", out var accessor);
            var handler = BuildHandler(new AdminCertificateSettings(), accessor.Object);

            await handler.HandleAsync(context);

            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenIssuerNotAllowed()
        {
            var certificate = CreateSelfSignedCertificate("CN=Wrong CA");
            var context = BuildAuthorizationContext(certificate, "admin@example.com", out var accessor);
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
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_Succeeds_WhenUserAndThumbprintMatch()
        {
            var certificate = CreateSelfSignedCertificate("CN=Admin CA");
            var context = BuildAuthorizationContext(certificate, "admin@example.com", out var accessor);
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
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
            _auditService.Verify(
                audit => audit.LogAuthenticationAttempt(
                    It.IsAny<HttpContext>(),
                    It.IsAny<ClaimsPrincipal>(),
                    true,
                    It.Is<string>(detail => detail.Contains("Admin"))),
                Times.Once);
        }

        private AdminCertificateRequirementHandler BuildHandler(
            AdminCertificateSettings settings,
            IHttpContextAccessor accessor)
        {
            return new AdminCertificateRequirementHandler(
                accessor,
                settings,
                _auditService.Object,
                _logger.Object);
        }

        private static AuthorizationHandlerContext BuildAuthorizationContext(
            X509Certificate2? certificate,
            string userIdentifier,
            out Mock<IHttpContextAccessor> accessor)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.ClientCertificate = certificate;

            accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(value => value.HttpContext).Returns(httpContext);

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Email, userIdentifier) },
                "Test"));

            return new AuthorizationHandlerContext(
                new[] { new AdminCertificateRequirement() },
                user,
                httpContext);
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
