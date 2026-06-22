using System.Collections.Concurrent;
using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Services
{
    public interface INonceCatalogService {

        public bool AddANonce(string whichOne, Nonce nonce);
        public string GetANonce(string whichOne);
        public bool RemoveANonce(string whichOne);

    }


    /// <summary>
    /// Utility class to generate and hold nonces in key-value pair dictionary.
    /// </summary>
    public class NonceCatalogService : INonceCatalogService
    {
        // In-memory storage for the nonces — ConcurrentDictionary is safe for concurrent reads and writes
        private static readonly ConcurrentDictionary<string, Nonce> _nonceCollection = new ConcurrentDictionary<string, Nonce>();

        // Logging for the service
        private readonly ILogger<NonceCatalogService> _logger ;

        // add a nonce by given name to a dictionary
        // retrieve a nonce by a given name from a dictionary

        public NonceCatalogService(ILogger<NonceCatalogService> logger) { 
        
            _logger = logger;

        }

        /// <summary>
        /// Add a Nonce object which should contain a generated value to the collection.
        /// </summary>
        /// <param name="whichOne"></param>
        /// <param name="nonce"></param>
        /// <returns></returns>
        public bool AddANonce(string whichOne, Nonce nonce)
        {
            _nonceCollection[whichOne] = nonce;
            return _nonceCollection.TryGetValue(whichOne, out _);
        }

        /// <summary>
        /// Get a Nonce object by a known key
        /// </summary>
        /// <param name="whichOne"></param>
        /// <returns>string representation of the nonce value</returns>
        public string GetANonce(string whichOne)
        {
            // Use TryGetValue with the out parameter to atomically retrieve the value.
            // A two-step check-then-lookup is not thread-safe even with ConcurrentDictionary
            // because another thread may remove the key between the check and the lookup.
            Nonce? nonce;
            string result;

            if (_nonceCollection.TryGetValue(whichOne, out nonce))
            {
                result = nonce.GetNonceAsString();
            }
            else
            {
                result = string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Remove a Nonce object by a known key.
        /// </summary>
        /// <param name="whichOne"></param>
        /// <returns>True if the collection no longer holds that key.</returns>
        public bool RemoveANonce(string whichOne)
        {
            return _nonceCollection.TryRemove(whichOne, out _);
        }
    }
}
