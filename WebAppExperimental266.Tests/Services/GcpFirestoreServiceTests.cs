using Google.Cloud.Firestore;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="GcpFirestoreService"/>.
    /// Because <see cref="FirestoreDb"/> is a sealed class with no public constructor,
    /// a <see cref="FirestoreDb"/> is built by pointing the Firestore client at a local emulator
    /// endpoint via the <c>FIRESTORE_EMULATOR_HOST</c> environment variable.  The emulator
    /// does not need to be running — the tests only exercise in-memory service methods
    /// (<see cref="GcpFirestoreService.GetCollectionName"/> and
    /// <see cref="GcpFirestoreService.GetDatabase"/>) that do not perform any network I/O.
    /// </summary>
    public class GcpFirestoreServiceTests
    {
        /// <summary>
        /// Creates a <see cref="FirestoreDb"/> aimed at the Firestore emulator (no emulator
        /// process required — the builder succeeds even without a live endpoint).
        /// </summary>
        private static FirestoreDb BuildTestDb(string projectId = "test-project")
        {
            // Point the client at a local emulator address so that no real GCP credentials
            // are needed.  The environment variable is used exclusively within this call.
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:9099");
            var db = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
            }.Build();
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", null);
            return db;
        }

        [Fact]
        public void GetCollectionName_ReturnsConfiguredName()
        {
            var db = BuildTestDb();
            var service = new GcpFirestoreService(db, "my-collection");
            service.GetCollectionName().Should().Be("my-collection");
        }

        [Fact]
        public void GetDatabase_ReturnsFirestoreDbInstance()
        {
            var db = BuildTestDb("test-project");
            var service = new GcpFirestoreService(db, "my-collection");
            service.GetDatabase().Should().NotBeNull();
            service.GetDatabase().ProjectId.Should().Be("test-project");
        }

        [Fact]
        public void GetCollection_ReturnsCollectionReference()
        {
            var db = BuildTestDb();
            var service = new GcpFirestoreService(db, "my-collection");
            var collection = service.GetCollection();
            collection.Should().NotBeNull();
            collection.Id.Should().Be("my-collection");
        }

        [Theory]
        [InlineData("users")]
        [InlineData("orders")]
        [InlineData("sessions")]
        public void GetCollectionName_WithDifferentNames_ReturnsCorrectName(string collectionName)
        {
            var db = BuildTestDb();
            var service = new GcpFirestoreService(db, collectionName);
            service.GetCollectionName().Should().Be(collectionName);
        }
    }
}
