using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public class CrudRecordAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, CrudRecord>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AdminCertificateSettings _adminCertificateSettings;

        public CrudRecordAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            AdminCertificateSettings adminCertificateSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _adminCertificateSettings = adminCertificateSettings;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            CrudRecord resource)
        {
            if (IsCertificateAdmin(context.User))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var isOwner = string.Equals(
                resource.OwnerId,
                UserIdentityHelper.GetStableUserId(context.User),
                StringComparison.OrdinalIgnoreCase);

            if (string.Equals(requirement.Name, CrudRecordOperations.Read.Name, StringComparison.Ordinal))
            {
                if (isOwner || resource.IsPublic)
                {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }

            if (string.Equals(requirement.Name, CrudRecordOperations.Owner.Name, StringComparison.Ordinal) ||
                string.Equals(requirement.Name, CrudRecordOperations.Edit.Name, StringComparison.Ordinal) ||
                string.Equals(requirement.Name, CrudRecordOperations.Delete.Name, StringComparison.Ordinal))
            {
                if (isOwner)
                {
                    context.Succeed(requirement);
                }
            }

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
