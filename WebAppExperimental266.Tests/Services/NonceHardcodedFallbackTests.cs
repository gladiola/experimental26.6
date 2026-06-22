using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Tests that enforce the Critical #3 security fix: hardcoded fallback nonces removed from
    /// OptimizedNonceMiddleware.
    ///
    /// WHY THESE TESTS EXIST:
    /// The original OptimizedNonceMiddleware fell back to the string literals
    /// "bootstrap-nonce-placeholder", "fallback-nonce", and "error-fallback-nonce" when nonce
    /// generation failed or the catalog was empty.  These values are committed to source code and
    /// therefore known to any attacker with access to the repository.  An error condition (e.g.,
    /// Key Vault unavailable) would silently put a predictable, exploitable nonce into the CSP
    /// header, completely defeating the protection.
    ///
    /// WHAT MUST NOT CHANGE TO KEEP THIS FIX IN PLACE:
    /// - The strings "bootstrap-nonce-placeholder", "fallback-nonce", and "error-fallback-nonce"
    ///   must never be used as nonce values anywhere in the codebase.
    /// - All fallback paths must call Nonce.GenerateSecureNonce() (or equivalent CSPRNG) to
    ///   produce an unpredictable value at runtime.
    /// - Do not introduce any other hardcoded nonce literal, regardless of the circumstances.
    /// </summary>
    public class NonceHardcodedFallbackTests
    {
        private static readonly string[] ForbiddenNonces =
        {
            "bootstrap-nonce-placeholder",
            "fallback-nonce",
            "error-fallback-nonce"
        };

        private OptimizedNonceMiddleware BuildMiddleware(
            Mock<INonceRefresherService> mockRefresher,
            Mock<INonceCatalogService> mockCatalog,
            RequestDelegate? next = null)
        {
            var mockLogger = new Mock<ILogger<OptimizedNonceMiddleware>>();
            next ??= _ => Task.CompletedTask;
            return new OptimizedNonceMiddleware(next, mockRefresher.Object, mockCatalog.Object, mockLogger.Object);
        }

        /// <summary>
        /// Verifies that when the nonce catalog is empty (first boot before any nonce is generated),
        /// the middleware produces a cryptographically random nonce — not a hardcoded placeholder.
        /// </summary>
        [Fact]
        public async Task OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce()
        {
            // Arrange — catalog is always empty, refresher succeeds but catalog stays empty
            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.GetANonce(It.IsAny<string>())).Returns(string.Empty);

            var mockRefresher = new Mock<INonceRefresherService>();
            mockRefresher.Setup(r => r.RefreshNonceAsync()).ReturnsAsync(string.Empty);

            var middleware = BuildMiddleware(mockRefresher, mockCatalog);

            // Use a path that is in IgnorePaths so we exercise the "reuse existing" branch
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/css/site.css";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var nonce = httpContext.Items["Nonce"] as string;
            nonce.Should().NotBeNullOrEmpty("a nonce must always be set");
            ForbiddenNonces.Should().NotContain(nonce,
                "hardcoded nonce literals are known to attackers and must never be used (Critical #3)");

            // Verify it looks like a Base64-encoded random value (can be decoded to 16 bytes)
            var bytes = Convert.FromBase64String(nonce!);
            bytes.Should().HaveCount(16);
        }

        /// <summary>
        /// Verifies that when nonce generation returns empty for a page request,
        /// the middleware produces a cryptographically random fallback — not "fallback-nonce".
        /// </summary>
        [Fact]
        public async Task OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce()
        {
            // Arrange — refresher returns empty, catalog returns empty
            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.GetANonce(It.IsAny<string>())).Returns(string.Empty);

            var mockRefresher = new Mock<INonceRefresherService>();
            mockRefresher.Setup(r => r.RefreshNonceAsync()).ReturnsAsync(string.Empty);

            var middleware = BuildMiddleware(mockRefresher, mockCatalog);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Home/Index"; // page request, triggers nonce generation

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var nonce = httpContext.Items["Nonce"] as string;
            nonce.Should().NotBeNullOrEmpty();
            ForbiddenNonces.Should().NotContain(nonce,
                "hardcoded nonce literals are known to attackers and must never be used (Critical #3)");

            var bytes = Convert.FromBase64String(nonce!);
            bytes.Should().HaveCount(16);
        }

        /// <summary>
        /// Verifies that when nonce generation throws an exception,
        /// the middleware produces a cryptographically random error-path nonce — not "error-fallback-nonce".
        /// </summary>
        [Fact]
        public async Task OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce()
        {
            // Arrange — refresher throws
            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.GetANonce(It.IsAny<string>())).Returns(string.Empty);

            var mockRefresher = new Mock<INonceRefresherService>();
            mockRefresher.Setup(r => r.RefreshNonceAsync()).ThrowsAsync(new InvalidOperationException("Key Vault unavailable"));

            var middleware = BuildMiddleware(mockRefresher, mockCatalog);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Home/Index";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var nonce = httpContext.Items["Nonce"] as string;
            nonce.Should().NotBeNullOrEmpty();
            ForbiddenNonces.Should().NotContain(nonce,
                "hardcoded nonce literals are known to attackers and must never be used (Critical #3)");

            var bytes = Convert.FromBase64String(nonce!);
            bytes.Should().HaveCount(16);
        }

        /// <summary>
        /// Verifies that fallback nonces across multiple error conditions are unique (unpredictable).
        /// Identical fallback nonces would indicate a hardcoded or deterministic value.
        /// </summary>
        [Fact]
        public async Task OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique()
        {
            // Arrange
            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.GetANonce(It.IsAny<string>())).Returns(string.Empty);

            var mockRefresher = new Mock<INonceRefresherService>();
            mockRefresher.Setup(r => r.RefreshNonceAsync()).ThrowsAsync(new InvalidOperationException("Key Vault unavailable"));

            var middleware = BuildMiddleware(mockRefresher, mockCatalog);

            // Act — run the middleware 50 times to collect fallback nonces
            var nonces = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                var httpContext = new DefaultHttpContext();
                httpContext.Request.Path = "/Home/Index";
                await middleware.InvokeAsync(httpContext);
                var nonce = httpContext.Items["Nonce"] as string;
                nonce.Should().NotBeNull();
                nonces.Add(nonce!);
            }

            // Assert — every nonce must be unique (hardcoded strings would collapse to count 1)
            nonces.Should().HaveCount(50,
                "each fallback nonce must be cryptographically unique; " +
                "identical nonces indicate a hardcoded or deterministic fallback (Critical #3)");
        }
    }
}
