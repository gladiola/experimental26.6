using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.User;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class LoggingHelperPiiHashTests
    {
        // ------------------------------------------------------------------ helpers

        private static Mock<ILogger> CreateLogger() => new Mock<ILogger>();

        private static string? CaptureFirstLogMessage(Mock<ILogger> mockLogger)
        {
            var calls = mockLogger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Log))
                .ToList();

            if (calls.Count == 0) return null;

            // The third argument to ILogger.Log is the state object; ToString() gives the rendered message.
            return calls[0].Arguments[2]?.ToString();
        }

        // ------------------------------------------------------------------ Initialize

        [Fact]
        public void Initialize_WithValidKey_DoesNotThrow()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            var act = () => LoggingHelper.Initialize(key);
            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithNullKey_DoesNotThrow()
        {
            var act = () => LoggingHelper.Initialize(null);
            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithEmptyKey_DoesNotThrow()
        {
            var act = () => LoggingHelper.Initialize(Array.Empty<byte>());
            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------ LogUserClaims — no PII in output

        [Fact]
        public void LogUserClaims_DoesNotLogPlaintextEmail()
        {
            // Arrange
            LoggingHelper.Initialize(RandomNumberGenerator.GetBytes(32));
            var mockLogger = CreateLogger();
            var userClaims = new UserClaims
            {
                Sid = "sid-abc",
                Oid = "oid-123",
                Name = "Alice Smith",
                Email = "alice@example.com",
                Roles = new[] { "Admin" }
            };

            // Act
            LoggingHelper.LogUserClaims(userClaims, mockLogger.Object, "TestMethod");

            // Assert — none of the logged messages should contain the raw email
            var allMessages = mockLogger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Log))
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();

            allMessages.Should().NotBeEmpty();
            allMessages.Should().NotContain(m => m.Contains("alice@example.com"),
                "email address must not appear in plaintext in log output");
        }

        [Fact]
        public void LogUserClaims_DoesNotLogPlaintextName()
        {
            LoggingHelper.Initialize(RandomNumberGenerator.GetBytes(32));
            var mockLogger = CreateLogger();
            var userClaims = new UserClaims
            {
                Sid = "sid-abc",
                Oid = "oid-123",
                Name = "Alice Smith",
                Email = "alice@example.com",
                Roles = new[] { "Viewer" }
            };

            LoggingHelper.LogUserClaims(userClaims, mockLogger.Object, "TestMethod");

            var allMessages = mockLogger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Log))
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();

            allMessages.Should().NotContain(m => m.Contains("Alice Smith"),
                "display name must not appear in plaintext in log output");
        }

        [Fact]
        public void LogUserClaims_DoesNotLogPlaintextOid()
        {
            LoggingHelper.Initialize(RandomNumberGenerator.GetBytes(32));
            var mockLogger = CreateLogger();
            var userClaims = new UserClaims
            {
                Oid = "unique-oid-value-xyz",
                Roles = Array.Empty<string>()
            };

            LoggingHelper.LogUserClaims(userClaims, mockLogger.Object, "TestMethod");

            var allMessages = mockLogger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Log))
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();

            allMessages.Should().NotContain(m => m.Contains("unique-oid-value-xyz"),
                "OID must not appear in plaintext in log output");
        }

        // ------------------------------------------------------------------ hash consistency

        [Fact]
        public void LogUserClaims_SameInputProducesSameHashToken_WithStableKey()
        {
            // Arrange — use a fixed key so hashes are deterministic
            var fixedKey = new byte[32];
            new Random(42).NextBytes(fixedKey);
            LoggingHelper.Initialize(fixedKey);

            var mockLogger1 = CreateLogger();
            var mockLogger2 = CreateLogger();

            var userClaims = new UserClaims
            {
                Sid = "sid-stable",
                Oid = "oid-stable",
                Name = "Bob",
                Email = "bob@example.com",
                Roles = new[] { "User" }
            };

            // Act
            LoggingHelper.LogUserClaims(userClaims, mockLogger1.Object, "M");
            LoggingHelper.LogUserClaims(userClaims, mockLogger2.Object, "M");

            // Assert — both runs produce the same log line
            var msg1 = mockLogger1.Invocations
                .First(i => i.Method.Name == nameof(ILogger.Log))
                .Arguments[2]?.ToString();

            var msg2 = mockLogger2.Invocations
                .First(i => i.Method.Name == nameof(ILogger.Log))
                .Arguments[2]?.ToString();

            msg1.Should().Be(msg2, "same PII with the same key must produce the same hash token");
        }

        [Fact]
        public void LogUserClaims_DifferentInputsProduceDifferentHashTokens()
        {
            var fixedKey = new byte[32];
            new Random(99).NextBytes(fixedKey);
            LoggingHelper.Initialize(fixedKey);

            var mockLogger1 = CreateLogger();
            var mockLogger2 = CreateLogger();

            var claims1 = new UserClaims { Oid = "oid-aaa", Email = "a@example.com", Roles = Array.Empty<string>() };
            var claims2 = new UserClaims { Oid = "oid-bbb", Email = "b@example.com", Roles = Array.Empty<string>() };

            LoggingHelper.LogUserClaims(claims1, mockLogger1.Object, "M");
            LoggingHelper.LogUserClaims(claims2, mockLogger2.Object, "M");

            var msg1 = mockLogger1.Invocations
                .First(i => i.Method.Name == nameof(ILogger.Log))
                .Arguments[2]?.ToString();

            var msg2 = mockLogger2.Invocations
                .First(i => i.Method.Name == nameof(ILogger.Log))
                .Arguments[2]?.ToString();

            msg1.Should().NotBe(msg2, "different PII values must produce different hash tokens");
        }

        // ------------------------------------------------------------------ null / empty PII

        [Fact]
        public void LogUserClaims_NullPiiFields_DoesNotThrow()
        {
            LoggingHelper.Initialize(RandomNumberGenerator.GetBytes(32));
            var mockLogger = CreateLogger();
            var userClaims = new UserClaims
            {
                Sid = null,
                Oid = null,
                Name = null,
                Email = null,
                Roles = null
            };

            var act = () => LoggingHelper.LogUserClaims(userClaims, mockLogger.Object, "M");
            act.Should().NotThrow();
        }
    }
}
