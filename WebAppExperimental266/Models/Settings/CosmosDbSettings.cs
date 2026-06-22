namespace WebAppExperimental266.Models.Settings
{
    public class CosmosDbSettings
    {

        /// <summary>
        /// Object to hold critical values for establishing communications with CosmosDB.
        /// Intended to work with appsettings.json values or similar.
        /// </summary>
        public CosmosDbSettings() { }

        public required string AccountEndpoint { get; set; }
        public required string AccountKey { get; set; }
        public required string CosmosConnectionString { get; set; }
        public required string DatabaseName { get; set; }
        public required string ContainerName { get; set; }
        public required string CommonPartitionKey { get; set; }

    }
}
