using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class CrudRecordAuthorizationHandlerTests
    {
        [Fact]
        public async Task HandleAsync_Succeeds_ForOwnerEdit()
        {
            var handler = BuildHandler(new AdminCertificateSettings(), null, BuildUser(("oid", "user-1")));
            var resource = new CrudRecord { OwnerId = "user-1", IsPublic = false };
            var context = BuildContext(CrudRecordOperations.Edit, handler.User, resource);

            await handler.Handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_Fails_ForNonOwnerEdit()
        {
            var handler = BuildHandler(new AdminCertificateSettings(), null, BuildUser(("oid", "user-2")));
            var resource = new CrudRecord { OwnerId = "user-1", IsPublic = false };
            var context = BuildContext(CrudRecordOperations.Edit, handler.User, resource);

            await handler.Handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_Succeeds_ForPublicRead_WhenNotOwner()
        {
            var handler = BuildHandler(new AdminCertificateSettings(), null, BuildUser(("oid", "user-2")));
            var resource = new CrudRecord { OwnerId = "user-1", IsPublic = true };
            var context = BuildContext(CrudRecordOperations.Read, handler.User, resource);

            await handler.Handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_Succeeds_ForSameGroupRead_WhenNotOwner()
        {
            var groupSettings = new GroupAccessSettings
            {
                UserAssignments =
                {
                    new GroupUserAssignment
                    {
                        GroupId = "group-a",
                        UserIdentifiers = new List<string> { "user-2" }
                    }
                }
            };
            var handler = BuildHandler(new AdminCertificateSettings(), null, BuildUser(("oid", "user-2")), groupSettings);
            var resource = new CrudRecord { OwnerId = "user-1", IsPublic = false, GroupId = "group-a" };
            var context = BuildContext(CrudRecordOperations.Read, handler.User, resource);

            await handler.Handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_Succeeds_ForMappedAdmin()
        {
            var certificate = CreateSelfSignedCertificate("CN=Admin CA");
            var user = BuildUser((ClaimTypes.Email, "admin@example.com"), ("oid", "admin-oid"));
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

            var handler = BuildHandler(settings, certificate, user);
            var resource = new CrudRecord { OwnerId = "someone-else", IsPublic = false };
            var context = BuildContext(CrudRecordOperations.Delete, user, resource);

            await handler.Handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        private static AuthorizationHandlerContext BuildContext(
            OperationAuthorizationRequirement requirement,
            ClaimsPrincipal user,
            CrudRecord resource) =>
            new(new[] { requirement }, user, resource);

        private static (CrudRecordAuthorizationHandler Handler, ClaimsPrincipal User) BuildHandler(
            AdminCertificateSettings settings,
            X509Certificate2? certificate,
            ClaimsPrincipal user,
            GroupAccessSettings? groupSettings = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.ClientCertificate = certificate;
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            return (new CrudRecordAuthorizationHandler(accessor, settings, groupSettings ?? new GroupAccessSettings()), user);
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
