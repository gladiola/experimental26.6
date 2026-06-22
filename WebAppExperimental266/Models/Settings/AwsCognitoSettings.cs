namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for AWS Cognito identity management.
    /// Mirrors the role of <see cref="AzureADSettings"/> for Microsoft Entra ID / Azure AD.
    /// </summary>
    public class AwsCognitoSettings
    {
        /// <summary>AWS region hosting the Cognito User Pool (e.g. "us-east-1").</summary>
        public required string Region { get; set; }

        /// <summary>
        /// Cognito User Pool ID (e.g. "us-east-1_AbCdEfGhI").
        /// Used to build the OIDC authority URL:
        /// https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}
        /// </summary>
        public required string UserPoolId { get; set; }

        /// <summary>
        /// Cognito App Client ID — the OAuth 2.0 client identifier assigned to this application.
        /// Equivalent to the Azure AD ClientId.
        /// </summary>
        public required string AppClientId { get; set; }

        /// <summary>
        /// Cognito App Client Secret.
        /// Store in User Secrets or environment variables — never in source control.
        /// Equivalent to the Azure AD ClientSecret.
        /// </summary>
        public required string AppClientSecret { get; set; }

        /// <summary>
        /// Cognito hosted-UI domain (e.g. "my-app.auth.us-east-1.amazoncognito.com").
        /// Used as the base for the Cognito logout endpoint.
        /// </summary>
        public required string Domain { get; set; }

        /// <summary>Callback path for the OIDC sign-in redirect. Defaults to "/signin-aws-cognito".</summary>
        public string CallbackPath { get; set; } = "/signin-aws-cognito";

        /// <summary>Callback path for the post-logout redirect. Defaults to "/signout-aws-cognito".</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-aws-cognito";
    }
}
