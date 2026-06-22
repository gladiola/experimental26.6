namespace WebAppExperimental266.Models.Settings
{
    public class ClientCredential
    {
        public required string SourceType { get; set; }
        public required string ClientSecret { get; set; }
    }

    public class AzureADSettings
    {
        public required string Authority { get; set; }
        public required string Instance {  get; set; }
        public required string Domain { get; set; }
        public required string TenantId { get; set; }
        public required string ClientId { get; set; }
        public required List<ClientCredential> ClientCredentials { get; set; }
        public string CallbackPath { get; set; } = "/signin-oidc";
        public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
    }
}

