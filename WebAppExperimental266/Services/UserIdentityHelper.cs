using System.Security.Claims;

namespace WebAppExperimental266.Services
{
    public static class UserIdentityHelper
    {
        private static readonly string[] CandidateClaimTypes =
        {
            "oid",
            ClaimTypes.NameIdentifier,
            "sub",
            ClaimTypes.Upn,
            ClaimTypes.Email,
            "preferred_username",
            ClaimTypes.Name
        };

        public static string GetStableUserId(ClaimsPrincipal user)
        {
            return GetCandidateIdentifiers(user).FirstOrDefault()
                ?? user.Identity?.Name
                ?? "anonymous";
        }

        public static IReadOnlyList<string> GetCandidateIdentifiers(ClaimsPrincipal user)
        {
            return CandidateClaimTypes
                .Select(claimType => user.FindFirstValue(claimType))
                .Append(user.Identity?.Name)
                .OfType<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string GetDisplayName(ClaimsPrincipal user)
        {
            return user.Identity?.Name
                ?? user.FindFirstValue("preferred_username")
                ?? user.FindFirstValue(ClaimTypes.Email)
                ?? GetStableUserId(user);
        }
    }
}
