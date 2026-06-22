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

            switch (requirement.Name)
            {
                case nameof(CrudRecordOperations.Read):
                    if (isOwner || resource.IsPublic)
                    {
                        context.Succeed(requirement);
                    }
                    break;
                case nameof(CrudRecordOperations.Owner):
                case nameof(CrudRecordOperations.Edit):
                case nameof(CrudRecordOperations.Delete):
                    if (isOwner)
                    {
                        context.Succeed(requirement);
                    }
                    break;
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
