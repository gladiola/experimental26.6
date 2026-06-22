using System.Security.Claims;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Models.Settings
{
    public class AdminCertificateSettings
    {
        public List<string> AllowedIssuers { get; set; } = new List<string>();

        public List<AdminCertificateUserMapping> AdminUsers { get; set; } = new List<AdminCertificateUserMapping>();

        public bool IsIssuerAllowed(string issuer)
        {
            if (string.IsNullOrWhiteSpace(issuer) || AllowedIssuers.Count == 0)
            {
                return false;
            }

            return AllowedIssuers.Any(allowed =>
                issuer.Contains(allowed, StringComparison.OrdinalIgnoreCase));
        }

        public AdminCertificateUserMapping? FindAuthorizedAdmin(ClaimsPrincipal user, string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            var normalizedThumbprint = NormalizeThumbprint(thumbprint);
            var userIdentifiers = UserIdentityHelper.GetCandidateIdentifiers(user);

            return AdminUsers.FirstOrDefault(admin =>
                admin.UserIdentifiers.Any(identifier =>
                    userIdentifiers.Contains(identifier, StringComparer.OrdinalIgnoreCase)) &&
                admin.AllowedThumbprints.Any(allowedThumbprint =>
                    string.Equals(
                        NormalizeThumbprint(allowedThumbprint),
                        normalizedThumbprint,
                        StringComparison.OrdinalIgnoreCase)));
        }

        public static string NormalizeThumbprint(string? thumbprint)
        {
            return string.IsNullOrWhiteSpace(thumbprint)
                ? string.Empty
                : thumbprint.Replace(":", string.Empty)
                            .Replace(" ", string.Empty)
                            .Trim()
                            .ToUpperInvariant();
        }
    }

    public class AdminCertificateUserMapping
    {
        public string DisplayName { get; set; } = string.Empty;

        public List<string> UserIdentifiers { get; set; } = new List<string>();

        public List<string> AllowedThumbprints { get; set; } = new List<string>();
    }
}
