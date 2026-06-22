namespace WebAppExperimental266.Models.Settings
{

    /// <summary>
    /// Class to hold configuration values for communicating with an Azure Key Vault.
    /// </summary>
    public class KeyVaultSettings
    {
        public required string KeyVaultURL { get; set; }
        public required string KeyVaultSecret { get; set; }
        public required string KeyVaultPassName { get; set; }
    }
}
