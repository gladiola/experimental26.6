using Microsoft.Azure.Cosmos;

namespace WebAppExperimental266.Services
{
    public class CosmosDbService
    {
        
        private readonly Database _database;

        public CosmosDbService(CosmosClient client, string databaseName)
        {
            _database = client.GetDatabase(databaseName);
        }

        public async Task<Database> GetDatabaseAsync()
        {
            return await _database.ReadAsync();
        }

        public string GetDatabaseId()
        {
            return _database.Id;
        }


    }
}
