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
        private readonly GroupAccessSettings _groupAccessSettings;

        public CrudRecordAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            AdminCertificateSettings adminCertificateSettings,
            GroupAccessSettings groupAccessSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _adminCertificateSettings = adminCertificateSettings;
            _groupAccessSettings = groupAccessSettings;
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
            var certificateIssuer = _httpContextAccessor.HttpContext?.Connection.ClientCertificate?.Issuer;
            var groupId = _groupAccessSettings.ResolveUserGroup(context.User, certificateIssuer);
            var isInSameGroup = !string.IsNullOrWhiteSpace(groupId)
                && !string.IsNullOrWhiteSpace(resource.GroupId)
                && string.Equals(groupId, resource.GroupId, StringComparison.OrdinalIgnoreCase);

            switch (requirement.Name)
            {
                case nameof(CrudRecordOperations.Read):
                    if (isOwner || resource.IsPublic || isInSameGroup)
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
