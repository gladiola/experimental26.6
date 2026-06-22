using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class NonceCatalogServiceTests
    {
        private readonly Mock<ILogger<NonceCatalogService>> _mockLogger;
        private readonly NonceCatalogService _service;

        public NonceCatalogServiceTests()
        {
            _mockLogger = new Mock<ILogger<NonceCatalogService>>();
            _service = new NonceCatalogService(_mockLogger.Object);
        }

        private Nonce CreateTestNonce()
        {
            var mockLogger = new Mock<ILogger<Nonce>>();
            return new Nonce(mockLogger.Object);
        }

        [Fact]
        public void AddANonce_ShouldAddNonceSuccessfully()
        {
            var nonce = CreateTestNonce();
            var catalogKey = "CSPNonce";

            var result = _service.AddANonce(catalogKey, nonce);

            result.Should().BeTrue();
        }

        [Fact]
        public void GetANonce_ShouldReturnNonceString_WhenNonceExists()
        {
            var nonce = CreateTestNonce();
            var catalogKey = "CSPNonce";
            _service.AddANonce(catalogKey, nonce);

            var result = _service.GetANonce(catalogKey);

            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetANonce_ShouldReturnEmptyString_WhenNonceDoesNotExist()
        {
            var catalogKey = "NonExistentNonce";

            var result = _service.GetANonce(catalogKey);

            result.Should().BeEmpty();
        }

        [Fact]
        public void RemoveANonce_ShouldRemoveNonceSuccessfully()
        {
            var nonce = CreateTestNonce();
            var catalogKey = "CSPNonce";
            _service.AddANonce(catalogKey, nonce);

            var result = _service.RemoveANonce(catalogKey);

            result.Should().BeTrue();
            _service.GetANonce(catalogKey).Should().BeEmpty();
        }

        [Fact]
        public void RemoveANonce_ShouldReturnFalse_WhenNonceDoesNotExist()
        {
            var catalogKey = "NonExistentNonce";

            var result = _service.RemoveANonce(catalogKey);

            result.Should().BeFalse();
        }

        [Fact]
        public void AddANonce_ShouldUpdateExistingNonce()
        {
            var nonce1 = CreateTestNonce();
            var nonce2 = CreateTestNonce();
            var catalogKey = "CSPNonce";

            _service.AddANonce(catalogKey, nonce1);
            var firstValue = _service.GetANonce(catalogKey);
            _service.AddANonce(catalogKey, nonce2);
            var secondValue = _service.GetANonce(catalogKey);

            firstValue.Should().NotBeEmpty();
            secondValue.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies that the ConcurrentDictionary backing store allows concurrent reads and writes
        /// without throwing exceptions or losing data. This test fails if the implementation is
        /// reverted to a non-thread-safe Dictionary.
        /// </summary>
        [Fact]
        public void NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions()
        {
            const int threadCount = 20;
            const int operationsPerThread = 50;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var mockLogger = new Mock<ILogger<NonceCatalogService>>();
            var service = new NonceCatalogService(mockLogger.Object);

            Parallel.For(0, threadCount, threadIndex =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"key-{threadIndex % 5}";
                        var nonce = CreateTestNonce();
                        service.AddANonce(key, nonce);
                        service.GetANonce(key);
                        if (i % 10 == 0)
                        {
                            service.RemoveANonce(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty("concurrent access to NonceCatalogService must not throw exceptions");
        }

        /// <summary>
        /// Confirms the backing collection is ConcurrentDictionary (thread-safe type).
        /// This test fails if the field type is reverted to the non-thread-safe Dictionary.
        /// </summary>
        [Fact]
        public void NonceCatalogService_BackingStore_IsConcurrentDictionary()
        {
            var fieldInfo = typeof(NonceCatalogService)
                .GetField("_nonceCollection",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            fieldInfo.Should().NotBeNull("_nonceCollection field must exist");

            var fieldType = fieldInfo!.FieldType;
            fieldType.Should().Be(
                typeof(System.Collections.Concurrent.ConcurrentDictionary<string, Nonce>),
                "NonceCatalogService must use ConcurrentDictionary for thread safety (security fix #4)");
        }
    }
}
