using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Unit tests for YubiKeyRequirementHandler.
    /// </summary>
    public class YubiKeyRequirementHandlerTests
    {
        private readonly Mock<ILogger<YubiKeyRequirementHandler>> _logger;
        private readonly Mock<IOcspValidationService> _ocspService;
        private readonly Mock<IWebHostEnvironment> _env;

        public YubiKeyRequirementHandlerTests()
        {
            _logger = new Mock<ILogger<YubiKeyRequirementHandler>>();
            _ocspService = new Mock<IOcspValidationService>();
            _env = new Mock<IWebHostEnvironment>();

            // Default: non-development environment
            _env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

            // Default: OCSP says certificate is valid
            _ocspService
                .Setup(s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()))
                .ReturnsAsync(true);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static X509Certificate2 CreateSelfSignedCert(string subjectCn, string issuerCn)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                $"CN={subjectCn}",
                key,
                HashAlgorithmName.SHA256);

            // For testing we create a self-signed cert but set the issuer via the subject
            // of the signing cert (which in self-signed is the same as subject).
            // We override the issuer CN by embedding it in the subject when we want the
            // issuer to look different — in practice the handler reads cert.Issuer.
            return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddYears(1));
        }

        private static AuthorizationHandlerContext BuildContext(
            X509Certificate2? clientCert,
            out Mock<IHttpContextAccessor> httpContextAccessorMock)
        {
            var httpContext = new DefaultHttpContext();
            if (clientCert != null)
                httpContext.Connection.ClientCertificate = clientCert;

            httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "test-user") },
                "Test"));

            return new AuthorizationHandlerContext(
                new[] { new YubiKeyRequirement() },
                user,
                httpContext);
        }

        private YubiKeyRequirementHandler BuildHandler(YubiKeySettings settings, IHttpContextAccessor accessor)
        {
            return new YubiKeyRequirementHandler(accessor, settings, _ocspService.Object, _env.Object, _logger.Object);
        }

        // ── No certificate ───────────────────────────────────────────────────────

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenNoCertificatePresent()
        {
            var context = BuildContext(null, out var accessor);
            var handler = BuildHandler(new YubiKeySettings(), accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenHttpContextIsNull()
        {
            var accessorMock = new Mock<IHttpContextAccessor>();
            accessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var context = new AuthorizationHandlerContext(
                new[] { new YubiKeyRequirement() }, user, null);

            var handler = BuildHandler(new YubiKeySettings(), accessorMock.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        // ── Issuer checks ────────────────────────────────────────────────────────

        [Fact]
        public async Task HandleRequirementAsync_Succeeds_WhenAllowedCaIssuersIsEmpty()
        {
            // No issuer restriction — any cert should pass (if OCSP agrees)
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string>()  // no restriction
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenIssuerNotInAllowedList()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            // The self-signed cert's issuer is "CN=alice", which won't match
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "CN=OpenBSD Admin CA" }
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_Succeeds_WhenIssuerMatchesAllowedEntry()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            // Self-signed cert: issuer == subject == "CN=alice"
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "alice" }
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        // ── YubiKey attestation checks ───────────────────────────────────────────

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenAttestationRequiredAndIssuerDoesNotMatch()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            var settings = new YubiKeySettings
            {
                RequireYubiKeyAttestation = true,
                YubiKeyAttestationIssuer = "Yubico PIV Attestation"
                // cert.Issuer is "CN=alice", does not contain "Yubico PIV Attestation"
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_SkipsAttestation_WhenRequireAttestationIsFalse()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            var settings = new YubiKeySettings
            {
                RequireYubiKeyAttestation = false,
                YubiKeyAttestationIssuer = "Yubico PIV Attestation"
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            // No issuer restriction and OCSP passes → should succeed
            context.HasSucceeded.Should().BeTrue();
        }

        // ── OCSP revocation checks ───────────────────────────────────────────────

        [Fact]
        public async Task HandleRequirementAsync_Fails_WhenOcspReturnsRevoked()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            _ocspService
                .Setup(s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()))
                .ReturnsAsync(false);

            var handler = BuildHandler(new YubiKeySettings(), accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
            context.HasFailed.Should().BeTrue();
        }

        [Fact]
        public async Task HandleRequirementAsync_SkipsOcsp_InDevelopmentWhenFlagSet()
        {
            _env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            // OCSP would reject the cert — but we should skip it in development
            _ocspService
                .Setup(s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()))
                .ReturnsAsync(false);

            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            var settings = new YubiKeySettings { SkipOcspInDevelopment = true };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            // OCSP skipped → should succeed (no other failing checks)
            context.HasSucceeded.Should().BeTrue();
            _ocspService.Verify(
                s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleRequirementAsync_CallsOcsp_InDevelopmentWhenSkipFlagFalse()
        {
            _env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            _ocspService
                .Setup(s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()))
                .ReturnsAsync(false);

            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            var settings = new YubiKeySettings { SkipOcspInDevelopment = false };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasFailed.Should().BeTrue();
            _ocspService.Verify(
                s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleRequirementAsync_CallsOcsp_InProduction()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            var handler = BuildHandler(new YubiKeySettings { SkipOcspInDevelopment = true }, accessor.Object);

            await handler.HandleAsync(context);

            // Production env → OCSP must be called
            _ocspService.Verify(
                s => s.ValidateCertificateAsync(It.IsAny<X509Certificate2>()),
                Times.Once);
        }

        // ── Success path ─────────────────────────────────────────────────────────

        [Fact]
        public async Task HandleRequirementAsync_Succeeds_WhenAllChecksPass()
        {
            var cert = CreateSelfSignedCert("alice", "alice");
            var context = BuildContext(cert, out var accessor);

            // "alice" is in AllowedCaIssuers, OCSP returns valid
            var settings = new YubiKeySettings
            {
                AllowedCaIssuers = new List<string> { "alice" }
            };
            var handler = BuildHandler(settings, accessor.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
            context.HasFailed.Should().BeFalse();
        }
    }
}
