using Microsoft.AspNetCore.Authorization;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public class AdminCertificateRequirement : IAuthorizationRequirement
    {
    }

    public class AdminCertificateRequirementHandler : AuthorizationHandler<AdminCertificateRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AdminCertificateSettings _settings;
        private readonly IAdminCertificateAuditService _auditService;
        private readonly ILogger<AdminCertificateRequirementHandler> _logger;

        public AdminCertificateRequirementHandler(
            IHttpContextAccessor httpContextAccessor,
            AdminCertificateSettings settings,
            IAdminCertificateAuditService auditService,
            ILogger<AdminCertificateRequirementHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings;
            _auditService = auditService;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminCertificateRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            LoggingHelper.TrackFunctionCall(httpContext, "AdminCertificateRequirementHandler.HandleRequirementAsync");

            var certificate = httpContext?.Connection.ClientCertificate;
            if (certificate == null)
            {
                _auditService.LogAuthenticationAttempt(
                    httpContext,
                    context.User,
                    success: false,
                    detail: "No client certificate was presented for the admin route.");
                context.Fail(new AuthorizationFailureReason(this, "An admin certificate is required."));
                return Task.CompletedTask;
            }

            if (!_settings.IsIssuerAllowed(certificate.Issuer))
            {
                _logger.LogWarning(
                    "Admin certificate rejected due to issuer {Issuer}",
                    certificate.Issuer);
                _auditService.LogAuthenticationAttempt(
                    httpContext,
                    context.User,
                    success: false,
                    detail: $"Rejected issuer {certificate.Issuer}");
                context.Fail(new AuthorizationFailureReason(this, "The client certificate issuer is not trusted for admin access."));
                return Task.CompletedTask;
            }

            var adminUser = _settings.FindAuthorizedAdmin(context.User, certificate.Thumbprint ?? string.Empty);
            if (adminUser == null)
            {
                _auditService.LogAuthenticationAttempt(
                    httpContext,
                    context.User,
                    success: false,
                    detail: "The certificate thumbprint is not mapped to the current admin user.");
                context.Fail(new AuthorizationFailureReason(this, "The client certificate is not mapped to this administrator."));
                return Task.CompletedTask;
            }

            _auditService.LogAuthenticationAttempt(
                httpContext,
                context.User,
                success: true,
                detail: $"Mapped to admin profile '{adminUser.DisplayName}'.");

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
