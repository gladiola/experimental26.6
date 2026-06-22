using Google.Cloud.Firestore;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Service that wraps a <see cref="FirestoreDb"/> client and provides access to a Firestore collection.
    /// Mirrors the role of <see cref="CosmosDbService"/> for Azure Cosmos DB.
    /// </summary>
    public class GcpFirestoreService
    {
        private readonly FirestoreDb _database;
        private readonly string _collectionName;

        public GcpFirestoreService(FirestoreDb database, string collectionName)
        {
            _database = database;
            _collectionName = collectionName;
        }

        /// <summary>Returns a reference to the configured Firestore collection.</summary>
        public CollectionReference GetCollection()
        {
            return _database.Collection(_collectionName);
        }

        /// <summary>Returns the configured collection name.</summary>
        public string GetCollectionName()
        {
            return _collectionName;
        }

        /// <summary>Returns the underlying <see cref="FirestoreDb"/> instance.</summary>
        public FirestoreDb GetDatabase()
        {
            return _database;
        }
    }
}
