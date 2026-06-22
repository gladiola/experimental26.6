namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for communicating with Google Cloud Firestore.
    /// Mirrors the role of <see cref="CosmosDbSettings"/> for Azure Cosmos DB.
    /// </summary>
    public class GcpFirestoreSettings
    {
        /// <summary>Google Cloud project ID (e.g. "my-project-123456").</summary>
        public required string ProjectId { get; set; }

        /// <summary>
        /// Firestore database ID.  Use "(default)" for the default database.
        /// </summary>
        public string DatabaseId { get; set; } = "(default)";

        /// <summary>Name of the top-level collection to use (analogous to a Cosmos DB container).</summary>
        public required string CollectionName { get; set; }

        /// <summary>
        /// Optional path to a service account JSON credential file.
        /// When empty the client uses Application Default Credentials (ADC).
        /// </summary>
        public string CredentialFilePath { get; set; } = string.Empty;
    }
}
