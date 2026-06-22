using System.Security.Claims;

namespace WebAppExperimental266.Services
{
    public static class UserIdentityHelper
    {
        private static readonly string[] StableClaimTypes =
        {
            "oid",
            "sub",
            ClaimTypes.NameIdentifier
        };

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
            var stableIdentifier = StableClaimTypes
                .Select(claimType => user.FindFirstValue(claimType))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (!string.IsNullOrWhiteSpace(stableIdentifier))
            {
                return stableIdentifier;
            }

            if (user.Identity?.IsAuthenticated == true)
            {
                throw new InvalidOperationException(
                    "No stable identity claim (oid, sub, nameidentifier) was found for the authenticated user.");
            }

            return "anonymous";
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
