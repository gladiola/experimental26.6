using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Service that wraps an <see cref="IAmazonDynamoDB"/> client and provides access to a DynamoDB table.
    /// Mirrors the role of <see cref="CosmosDbService"/> for Azure Cosmos DB.
    /// </summary>
    public class AwsDynamoDbService
    {
        private readonly IAmazonDynamoDB _client;
        private readonly string _tableName;

        public AwsDynamoDbService(IAmazonDynamoDB client, string tableName)
        {
            _client = client;
            _tableName = tableName;
        }

        /// <summary>Returns the DynamoDB table description by calling <c>DescribeTable</c>.</summary>
        public async Task<TableDescription> GetTableAsync()
        {
            var response = await _client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = _tableName
            });
            return response.Table;
        }

        /// <summary>Returns the configured table name.</summary>
        public string GetTableName()
        {
            return _tableName;
        }
    }
}
