using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Razor;

namespace WebAppExperimental266.Models.User
{
    public class UserClaims
    {
        /// <summary>
        /// Value that represents a session from the sid claim.
        /// </summary>
        public string? Sid { get; set; }

        /// <summary>
        /// Represents a user's id from the oid claim.
        /// </summary>
        public string? Oid { get; set; }

        /// <summary>
        /// Represents a user's name from the name claim.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Represents a user's roles from the roles claim.
        /// </summary>
        public string[]? Roles { get; set; }

        /// <summary>
        /// Represents a user's email from the email claim.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Constructor to neutralize and load values from a User.Claims in Razor.
        /// Class to hold user identification data from JWT claims
        /// </summary>
        public UserClaims() { }
    }
}
