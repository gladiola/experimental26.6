namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration settings for OCSP (Online Certificate Status Protocol) server integration
    /// Used to validate certificate revocation status before processing web requests
    /// </summary>
    public class OcspSettings
    {
        /// <summary>
        /// Enable or disable OCSP certificate validation
        /// </summary>
        public bool EnableOcspValidation { get; set; } = false;

        /// <summary>
        /// URL of the OCSP responder server
        /// Example: "http://ocsp.example.com/ocsp" or "https://ocsp.example.com"
        /// </summary>
        public string? OcspServerUrl { get; set; }

        /// <summary>
        /// Timeout for OCSP requests in seconds
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed OCSP requests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Cache duration for OCSP responses in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Behavior when OCSP server is unavailable
        /// Options: "Fail" (reject request), "Allow" (continue processing), "Warn" (log warning but continue)
        /// </summary>
        public string ServerUnavailableBehavior { get; set; } = "Warn";

        /// <summary>
        /// Enable detailed logging for OCSP operations
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Skip OCSP validation for development environment
        /// </summary>
        public bool SkipValidationInDevelopment { get; set; } = true;
    }
}
