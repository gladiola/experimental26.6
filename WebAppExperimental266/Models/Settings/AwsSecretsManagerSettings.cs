namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for communicating with AWS Secrets Manager.
    /// Mirrors the role of <see cref="KeyVaultSettings"/> for Azure Key Vault.
    /// </summary>
    public class AwsSecretsManagerSettings
    {
        /// <summary>AWS region (e.g. "us-east-1").</summary>
        public required string Region { get; set; }

        /// <summary>Name or ARN of the secret that contains the server certificate (PFX, base64-encoded).</summary>
        public required string CertificateSecretName { get; set; }

        /// <summary>Name or ARN of the secret used as the nonce encryption IV.</summary>
        public required string IVSecretName { get; set; }

        /// <summary>Name or ARN of the secret used as the nonce encryption key.</summary>
        public required string NonceKeySecretName { get; set; }

        /// <summary>
        /// AWS Access Key ID.  Store in User Secrets or environment variables — never in source control.
        /// </summary>
        public required string AccessKeyId { get; set; }

        /// <summary>
        /// AWS Secret Access Key.  Store in User Secrets or environment variables — never in source control.
        /// </summary>
        public required string SecretAccessKey { get; set; }
    }
}
