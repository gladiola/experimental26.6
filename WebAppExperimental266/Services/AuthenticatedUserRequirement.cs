using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public class AuthenticatedUserRequirement : IAuthorizationRequirement
    {
    }

    public class AuthenticatedUserRequirementHandler : AuthorizationHandler<AuthenticatedUserRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AdminCertificateSettings _adminCertificateSettings;

        public AuthenticatedUserRequirementHandler(
            IHttpContextAccessor httpContextAccessor,
            AdminCertificateSettings adminCertificateSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _adminCertificateSettings = adminCertificateSettings;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AuthenticatedUserRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Fail(new AuthorizationFailureReason(this, "Authenticated user is required."));
                return Task.CompletedTask;
            }

            if (IsCertificateAdmin(context.User))
            {
                context.Fail(new AuthorizationFailureReason(this, "Admin certificate identities are not allowed on user record routes."));
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        private bool IsCertificateAdmin(ClaimsPrincipal user)
        {
            var certificate = _httpContextAccessor.HttpContext?.Connection.ClientCertificate;
            if (certificate == null)
            {
                return false;
            }

            if (!_adminCertificateSettings.IsIssuerAllowed(certificate.Issuer))
            {
                return false;
            }

            return _adminCertificateSettings.FindAuthorizedAdmin(user, certificate.Thumbprint ?? string.Empty) != null;
        }
    }
}
