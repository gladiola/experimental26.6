using WebAppExperimental266.Models.User;

namespace WebAppExperimental266.Services
{

    public interface IUserClaimsLoader
    {
        Task<UserClaims> LoadDataAsync(IEnumerable<System.Security.Claims.Claim> claims);
    }

    public class UserClaimsLoader : IUserClaimsLoader
    {
        public async Task<UserClaims> LoadDataAsync(IEnumerable<System.Security.Claims.Claim> Claims)
        {
            string? userClaimsSessionIdentifier;
            string? userClaimsUserIdentifier;
            string? userClaimsName;
            string[]? userClaimsRoles;
            string? userClaimsEmail;
            string vUserClaimsSessionIdentifier;
            string vUserClaimsUserIdentifier;
            string vUserClaimsName;
            string[] vUserClaimsRoles;
            string vUserClaimsEmail;

            await Task.Delay(1);

            userClaimsSessionIdentifier = Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
            userClaimsUserIdentifier = Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            userClaimsName = Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            userClaimsRoles = Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();
            userClaimsEmail = Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            // Ensure required values are not null.
            vUserClaimsSessionIdentifier = userClaimsSessionIdentifier ?? "None found.";
            vUserClaimsUserIdentifier = userClaimsUserIdentifier ?? "None found.";
            vUserClaimsName = userClaimsName ?? "None found.";

            if (userClaimsRoles.Length == 0)
            {
                vUserClaimsRoles = new string[1];
                vUserClaimsRoles[0] = "None Found";
            }
            else {
                vUserClaimsRoles = new string[userClaimsRoles.Length];
                vUserClaimsRoles = userClaimsRoles.ToArray();
            }

            vUserClaimsEmail = userClaimsEmail ?? "None found.";

            UserClaims uc = new()
            {

                Sid = vUserClaimsSessionIdentifier,
                Oid = vUserClaimsUserIdentifier,
                Name = vUserClaimsName,
                Roles = vUserClaimsRoles,
                Email = vUserClaimsEmail
            };

            return uc;
        }

    }
}
