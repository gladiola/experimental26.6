namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for Google Cloud Identity Platform (Google OAuth 2.0 / OpenID Connect).
    /// Mirrors the role of <see cref="AzureADSettings"/> for Microsoft Entra ID / Azure AD.
    /// </summary>
    public class GcpIdentitySettings
    {
        /// <summary>
        /// Google OAuth 2.0 Client ID obtained from the Google Cloud Console.
        /// Equivalent to the Azure AD ClientId.
        /// </summary>
        public required string ClientId { get; set; }

        /// <summary>
        /// Google OAuth 2.0 Client Secret.
        /// Store in User Secrets or environment variables — never in source control.
        /// Equivalent to the Azure AD ClientSecret.
        /// </summary>
        public required string ClientSecret { get; set; }

        /// <summary>
        /// Optional Google Cloud Project ID (e.g. "my-project-123456").
        /// Used for logging and informational purposes.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Callback path for the OIDC sign-in redirect. Defaults to "/signin-gcp".</summary>
        public string CallbackPath { get; set; } = "/signin-gcp";

        /// <summary>Callback path for the post-logout redirect. Defaults to "/signout-gcp".</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-gcp";
    }
}
