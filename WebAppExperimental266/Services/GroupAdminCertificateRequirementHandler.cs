using Microsoft.AspNetCore.Authorization;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public class GroupAdminCertificateRequirement : IAuthorizationRequirement
    {
    }

    public class GroupAdminCertificateRequirementHandler : AuthorizationHandler<GroupAdminCertificateRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GroupAccessSettings _groupAccessSettings;
        private readonly IAdminCertificateAuditService _auditService;

        public GroupAdminCertificateRequirementHandler(
            IHttpContextAccessor httpContextAccessor,
            GroupAccessSettings groupAccessSettings,
            IAdminCertificateAuditService auditService)
        {
            _httpContextAccessor = httpContextAccessor;
            _groupAccessSettings = groupAccessSettings;
            _auditService = auditService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GroupAdminCertificateRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var certificate = httpContext?.Connection.ClientCertificate;
            if (certificate == null)
            {
                _auditService.LogAuthenticationAttempt(
                    httpContext,
                    context.User,
                    success: false,
                    detail: "No client certificate was presented for group-admin route.");
                context.Fail(new AuthorizationFailureReason(this, "A group admin certificate is required."));
                return Task.CompletedTask;
            }

            var groupAdmin = _groupAccessSettings.FindAuthorizedGroupAdmin(
                context.User,
                certificate.Thumbprint ?? string.Empty,
                certificate.Issuer);
            if (groupAdmin == null)
            {
                _auditService.LogAuthenticationAttempt(
                    httpContext,
                    context.User,
                    success: false,
                    detail: "The certificate is not mapped to the current group-admin user.");
                context.Fail(new AuthorizationFailureReason(this, "The client certificate is not mapped to this group administrator."));
                return Task.CompletedTask;
            }

            _auditService.LogAuthenticationAttempt(
                httpContext,
                context.User,
                success: true,
                detail: $"Mapped to group admin profile '{groupAdmin.AdminDisplayName}' in group '{groupAdmin.GroupDisplayName}'.");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
