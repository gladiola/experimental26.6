using System.Security.Claims;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Models.Settings
{
    public class GroupAccessSettings
    {
        public List<GroupDefinition> Groups { get; set; } = new List<GroupDefinition>();

        public List<GroupUserAssignment> UserAssignments { get; set; } = new List<GroupUserAssignment>();

        public string? ResolveUserGroup(ClaimsPrincipal user, string? certificateIssuer)
        {
            var identifiers = UserIdentityHelper.GetCandidateIdentifiers(user);

            var assignedGroup = UserAssignments.FirstOrDefault(assignment =>
                assignment.UserIdentifiers.Any(identifier =>
                    identifiers.Contains(identifier, StringComparer.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(assignedGroup?.GroupId))
            {
                return assignedGroup.GroupId;
            }

            if (string.IsNullOrWhiteSpace(certificateIssuer))
            {
                return null;
            }

            return Groups
                .FirstOrDefault(group => group.IsIssuerMatch(certificateIssuer))
                ?.Id;
        }

        public GroupAdminAuthorization? FindAuthorizedGroupAdmin(
            ClaimsPrincipal user,
            string thumbprint,
            string issuer)
        {
            if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(issuer))
            {
                return null;
            }

            var normalizedThumbprint = AdminCertificateSettings.NormalizeThumbprint(thumbprint);
            var userIdentifiers = UserIdentityHelper.GetCandidateIdentifiers(user);

            foreach (var group in Groups.Where(group => group.IsIssuerMatch(issuer)))
            {
                var admin = group.GroupAdmins.FirstOrDefault(groupAdmin =>
                    groupAdmin.UserIdentifiers.Any(identifier =>
                        userIdentifiers.Contains(identifier, StringComparer.OrdinalIgnoreCase)) &&
                    groupAdmin.AllowedThumbprints.Any(allowedThumbprint =>
                        string.Equals(
                            AdminCertificateSettings.NormalizeThumbprint(allowedThumbprint),
                            normalizedThumbprint,
                            StringComparison.OrdinalIgnoreCase)));

                if (admin != null)
                {
                    return new GroupAdminAuthorization
                    {
                        GroupId = group.Id,
                        GroupDisplayName = group.DisplayName,
                        AdminDisplayName = admin.DisplayName
                    };
                }
            }

            return null;
        }
    }

    public class GroupDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string CertificateIssuerFragment { get; set; } = string.Empty;

        public List<GroupAdminUserMapping> GroupAdmins { get; set; } = new List<GroupAdminUserMapping>();

        public bool IsIssuerMatch(string issuer)
        {
            return !string.IsNullOrWhiteSpace(CertificateIssuerFragment)
                && !string.IsNullOrWhiteSpace(issuer)
                && issuer.Contains(CertificateIssuerFragment, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class GroupAdminUserMapping
    {
        public string DisplayName { get; set; } = string.Empty;

        public List<string> UserIdentifiers { get; set; } = new List<string>();

        public List<string> AllowedThumbprints { get; set; } = new List<string>();
    }

    public class GroupUserAssignment
    {
        public string GroupId { get; set; } = string.Empty;

        public List<string> UserIdentifiers { get; set; } = new List<string>();
    }

    public class GroupAdminAuthorization
    {
        public string GroupId { get; set; } = string.Empty;

        public string GroupDisplayName { get; set; } = string.Empty;

        public string AdminDisplayName { get; set; } = string.Empty;
    }
}
