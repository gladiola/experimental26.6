using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    /// <summary>
    /// Tests that enforce the Critical #2 security fix: nonce values must never be logged in plaintext.
    ///
    /// WHY THESE TESTS EXIST:
    /// The original NonceMiddleware and NonceRefresherService logged the actual nonce value in
    /// plaintext (e.g. "Nonce: {nonce}"). Anyone with read access to the application logs gains a
    /// valid, usable nonce and can trivially bypass the Content-Security-Policy by injecting an
    /// inline &lt;script nonce="..."&gt; tag with the known value.
    ///
    /// WHAT MUST NOT CHANGE TO KEEP THIS FIX IN PLACE:
    /// - Log messages must never include the nonce string itself.
    /// - Log messages may confirm success/failure of nonce generation but must not reveal the value.
    /// - If a new logging call is added anywhere in nonce-related code, review it to ensure it does
    ///   not include the nonce value in the message or any structured-logging parameter.
    /// </summary>
    public class NoncePlaintextLoggingTests
    {
        /// <summary>
        /// Verifies that NonceRefresherService logs a success message WITHOUT the nonce value.
        /// If the log message is reverted to include "Generated Nonce: {value}", this test fails.
        /// </summary>
        [Fact]
        public async Task NonceRefresherService_DoesNotLogNonceValue_OnSuccess()
        {
            // Arrange
            var logMessages = new List<string>();

            var mockLogger = new Mock<ILogger<NonceRefresherService>>();
            mockLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, id, state, ex, formatter) =>
                {
                    logMessages.Add(formatter.DynamicInvoke(state, ex)?.ToString() ?? string.Empty);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockNonceLogger = new Mock<ILogger<Nonce>>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockNonceLogger.Object);

            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.AddANonce(It.IsAny<string>(), It.IsAny<Nonce>())).Returns(true);

            var service = new NonceRefresherService(
                mockLogger.Object,
                mockLoggerFactory.Object,
                mockCatalog.Object);

            // Act
            var nonceValue = await service.RefreshNonceAsync();

            // Assert — the nonce value itself must not appear in any log message
            nonceValue.Should().NotBeNullOrEmpty("nonce must be generated");
            foreach (var msg in logMessages)
            {
                msg.Should().NotContain(nonceValue,
                    "the nonce value must never be written to logs (Critical #2: nonce logged in plaintext)");
            }
        }

        /// <summary>
        /// Verifies that NonceMiddleware logs a message WITHOUT the nonce value.
        /// If the log message is reverted to include "Nonce: {value}", this test fails.
        /// </summary>
        [Fact]
        public async Task NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync()
        {
            // Arrange
            var logMessages = new List<string>();

            var mockLogger = new Mock<ILogger<NonceMiddleware>>();
            mockLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, id, state, ex, formatter) =>
                {
                    logMessages.Add(formatter.DynamicInvoke(state, ex)?.ToString() ?? string.Empty);
                });

            const string expectedNonce = "test-nonce-value-abc123";
            var mockCatalog = new Mock<INonceCatalogService>();
            mockCatalog.Setup(c => c.GetANonce("CSPNonce")).Returns(expectedNonce);

            var mockRefresher = new Mock<INonceRefresherService>();
            mockRefresher.Setup(r => r.RefreshNonceAsync()).ReturnsAsync(expectedNonce);

            var mockAzureAdSettings = new Mock<IAzureADSettingsService>();

            RequestDelegate next = _ => Task.CompletedTask;
            var middleware = new NonceMiddleware(next, mockCatalog.Object, mockLogger.Object,
                mockRefresher.Object, mockAzureAdSettings.Object);

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert — the nonce value itself must not appear in any log message
            foreach (var msg in logMessages)
            {
                msg.Should().NotContain(expectedNonce,
                    "the nonce value must never be written to logs (Critical #2: nonce logged in plaintext)");
            }
        }
    }
}
