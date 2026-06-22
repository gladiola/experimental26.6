namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration values for communicating with Amazon DynamoDB.
    /// Mirrors the role of <see cref="CosmosDbSettings"/> for Azure Cosmos DB.
    /// </summary>
    public class AwsDynamoDbSettings
    {
        /// <summary>AWS region (e.g. "us-east-1").</summary>
        public required string Region { get; set; }

        /// <summary>Name of the DynamoDB table.</summary>
        public required string TableName { get; set; }

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
