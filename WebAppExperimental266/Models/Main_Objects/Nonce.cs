using Microsoft.Extensions.Logging;
using WebAppExperimental266.Services;
using System.Security.Cryptography;

namespace WebAppExperimental266.Models.Main_Objects
{
    public interface INonce
    {
        string GetNonceAsString();
    }

    /// <summary>
    /// Generates a cryptographically secure, unpredictable nonce for use as a CSP nonce.
    ///
    /// SECURITY NOTE — Why AES-GCM was removed:
    /// The previous implementation encrypted a random number using AES-GCM with a fixed IV that
    /// was retrieved from Key Vault on every call.  Reusing the same IV with the same key under
    /// AES-GCM is catastrophic: an attacker who observes two ciphertexts can XOR them to recover
    /// the XOR of the plaintexts, and the authentication tag can be forged.  More importantly,
    /// encrypting a nonce added no security benefit — a CSP nonce only needs to be unpredictable
    /// and unique per request.  <see cref="RandomNumberGenerator.GetBytes(int)"/> provides exactly
    /// that guarantee without any risk of IV reuse.
    ///
    /// HOW TO KEEP THIS FIX IN PLACE:
    /// 1. Never replace <see cref="GenerateSecureNonce"/> with an approach that reuses an IV.
    /// 2. Do not introduce a Key Vault IV/key pair for nonce generation — encryption is not needed.
    /// 3. The nonce byte length is 16 (128 bits), which yields a sufficiently large nonce space.
    ///    Do not reduce this length.
    /// 4. The nonce is Base64-encoded for inclusion in HTTP headers and HTML attributes.
    /// </summary>
    public class Nonce : INonce
    {
        private readonly string _nonce;
        private readonly ILogger<Nonce> _logger;

        /// <summary>
        /// Generates a fresh cryptographically random nonce.
        /// </summary>
        public Nonce(ILogger<Nonce> logger)
        {
            _logger = logger;
            _nonce = GenerateSecureNonce();
        }

        /// <summary>
        /// Generates a 16-byte cryptographically random nonce encoded as Base64.
        /// No encryption, no Key Vault dependency — randomness is the only security property needed.
        /// </summary>
        /// <returns>A Base64-encoded 16-byte random nonce string.</returns>
        public static string GenerateSecureNonce()
        {
            byte[] randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Accessor for nonce value.
        /// </summary>
        /// <returns>Base64-encoded nonce string.</returns>
        public string GetNonceAsString()
        {
            return _nonce;
        }
    }
}
