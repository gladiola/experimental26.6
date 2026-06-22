namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration settings for mutual TLS (mTLS) client certificate authentication
    /// </summary>
    public class MtlsSettings
    {
        /// <summary>
        /// Enable or disable client certificate validation
        /// </summary>
        public bool RequireClientCertificate { get; set; } = true;

        /// <summary>
        /// Allow chained certificates (not self-signed)
        /// </summary>
        public bool AllowCertificateChains { get; set; } = true;

        /// <summary>
        /// Allow self-signed certificates (for development only)
        /// </summary>
        public bool AllowSelfSignedCertificates { get; set; } = false;

        /// <summary>
        /// Perform certificate revocation check
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;

        /// <summary>
        /// Name of the client certificate in Azure Key Vault (optional)
        /// </summary>
        public string? ClientCertificateName { get; set; }

        /// <summary>
        /// Enable certificate issuer validation. When true, only certificates whose
        /// Issuer field contains at least one of the <see cref="AllowedIssuers"/> values
        /// will be accepted.
        /// </summary>
        public bool ValidateClientCertificateIssuer { get; set; } = true;

        /// <summary>
        /// One or more issuer distinguished-name substrings that an accepted client certificate
        /// must match (case-insensitive contains check). At least one entry is required when
        /// <see cref="ValidateClientCertificateIssuer"/> is true.
        /// </summary>
        public List<string> AllowedIssuers { get; set; } = new List<string>();

        /// <summary>
        /// Returns true if <paramref name="issuer"/> is permitted under the current settings.
        /// Always returns true when <see cref="ValidateClientCertificateIssuer"/> is false.
        /// Returns false when validation is on but <see cref="AllowedIssuers"/> is empty.
        /// </summary>
        public bool IsIssuerAllowed(string issuer)
        {
            if (!ValidateClientCertificateIssuer)
                return true;

            if (AllowedIssuers == null || AllowedIssuers.Count == 0)
                return false;

            return AllowedIssuers.Any(allowed =>
                issuer.Contains(allowed, StringComparison.OrdinalIgnoreCase));
        }
    }
}
