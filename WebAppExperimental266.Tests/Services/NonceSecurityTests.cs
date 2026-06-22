using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Tests that enforce the Critical #1 security fix: AES-GCM IV reuse removed from nonce generation.
    ///
    /// WHY THESE TESTS EXIST:
    /// The original Nonce implementation used AES-GCM with a fixed IV retrieved from Key Vault.
    /// Reusing the same IV with the same AES-GCM key is catastrophic — it allows an attacker to
    /// recover plaintext by XOR-ing two ciphertexts and to forge authentication tags.
    /// The fix replaces the entire AES-GCM approach with RandomNumberGenerator.GetBytes(16),
    /// which is the correct, standard way to generate a CSP nonce.
    ///
    /// WHAT MUST NOT CHANGE TO KEEP THIS FIX IN PLACE:
    /// - Nonce must not accept a KeyVault IV or key parameter.
    /// - Nonce generation must use RandomNumberGenerator (or equivalent CSPRNG).
    /// - Two successive nonces must be different (statistical certainty with 128-bit random).
    /// - The nonce must be valid Base64 (suitable for HTTP headers and HTML attributes).
    /// </summary>
    public class NonceSecurityTests
    {
        private Nonce CreateNonce()
        {
            var mockLogger = new Mock<ILogger<Nonce>>();
            return new Nonce(mockLogger.Object);
        }

        /// <summary>
        /// Verifies that Nonce no longer depends on AES-GCM Key Vault parameters.
        /// If the constructor is reverted to accept (ILogger, KeyVaultSecret, KeyVaultSecret)
        /// this test will fail to compile, immediately signaling the regression.
        /// </summary>
        [Fact]
        public void Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters()
        {
            // The new constructor takes only ILogger<Nonce>.
            // If code is reverted to require KeyVaultSecret IV + Key, this will fail to compile.
            var mockLogger = new Mock<ILogger<Nonce>>();
            var nonce = new Nonce(mockLogger.Object);
            nonce.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that a generated nonce is non-empty and valid Base64.
        /// </summary>
        [Fact]
        public void Nonce_GetNonceAsString_ReturnsNonEmptyBase64()
        {
            var nonce = CreateNonce();
            var value = nonce.GetNonceAsString();

            value.Should().NotBeNullOrEmpty("a nonce must always be generated");

            // Valid Base64 can be decoded without throwing
            var decoded = Convert.FromBase64String(value);
            decoded.Should().HaveCount(16, "nonce must be exactly 16 bytes (128 bits)");
        }

        /// <summary>
        /// Verifies that GenerateSecureNonce returns 16 bytes of Base64-encoded random data.
        /// </summary>
        [Fact]
        public void GenerateSecureNonce_Returns16ByteBase64()
        {
            var value = Nonce.GenerateSecureNonce();

            value.Should().NotBeNullOrEmpty();
            var bytes = Convert.FromBase64String(value);
            bytes.Should().HaveCount(16);
        }

        /// <summary>
        /// Verifies that successive nonces are unique (no IV reuse).
        /// With 128-bit random nonces the probability of collision in 1 000 draws is astronomically
        /// small — a collision would indicate the CSPRNG is broken or the IV is being reused.
        /// </summary>
        [Fact]
        public void Nonce_SuccessiveGenerations_AreUnique()
        {
            const int sampleSize = 1000;
            var values = new HashSet<string>();

            for (int i = 0; i < sampleSize; i++)
            {
                values.Add(Nonce.GenerateSecureNonce());
            }

            values.Should().HaveCount(sampleSize,
                "every nonce generation must produce a unique value; " +
                "a collision indicates IV reuse or a broken CSPRNG");
        }

        /// <summary>
        /// Verifies that the nonce contains sufficient entropy (at least 10 unique byte values
        /// across a batch of 20 nonces — trivially satisfied by any real CSPRNG).
        /// </summary>
        [Fact]
        public void Nonce_HasSufficientEntropy()
        {
            var allBytes = new List<byte>();
            for (int i = 0; i < 20; i++)
            {
                allBytes.AddRange(Convert.FromBase64String(Nonce.GenerateSecureNonce()));
            }

            var distinctByteValues = allBytes.Distinct().Count();
            distinctByteValues.Should().BeGreaterThan(10,
                "a CSPRNG should produce many distinct byte values; " +
                "very few distinct values indicates the entropy source is broken");
        }
    }
}
