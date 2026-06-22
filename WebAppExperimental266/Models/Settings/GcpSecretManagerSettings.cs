namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for communicating with Google Cloud Secret Manager.
    /// Mirrors the role of <see cref="KeyVaultSettings"/> for Azure Key Vault.
    /// </summary>
    public class GcpSecretManagerSettings
    {
        /// <summary>Google Cloud project ID (e.g. "my-project-123456").</summary>
        public required string ProjectId { get; set; }

        /// <summary>Secret ID of the server certificate secret (PFX, base64-encoded).</summary>
        public required string CertificateSecretId { get; set; }

        /// <summary>Secret ID of the nonce encryption IV secret.</summary>
        public required string IVSecretId { get; set; }

        /// <summary>Secret ID of the nonce encryption key secret.</summary>
        public required string NonceKeySecretId { get; set; }

        /// <summary>
        /// Optional path to a service account JSON credential file.
        /// When empty the client uses Application Default Credentials (ADC).
        /// </summary>
        public string CredentialFilePath { get; set; } = string.Empty;
    }
}
