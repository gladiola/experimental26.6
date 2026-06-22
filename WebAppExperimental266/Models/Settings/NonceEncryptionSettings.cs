namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Class to decsribe configuration values for encrypting a random number as nonce.
    /// </summary>
    public class NonceEncryptionSettings
    {
        public string? Key { get; set; }
        public string? IV { get; set; }
        public required string KeyVaultURL { get; set; }
        public required string IVSecret { get; set; }
        public required string NonceKeySecret { get; set; }
    }
}
