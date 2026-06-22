using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class LoggingMiddlewareTests
    {
        private readonly Mock<ILogger<LoggingMiddleware>> _mockLogger;
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly LoggingMiddleware _middleware;

        public LoggingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<LoggingMiddleware>>();
            _mockNext = new Mock<RequestDelegate>();
            _middleware = new LoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldAcceptDependencies()
        {
            // Assert
            _middleware.Should().NotBeNull();
        }

        [Fact]
        public async Task Invoke_ShouldLogBeforeAndAfterRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Response sent")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Invoke_ShouldCallNextDelegate()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new LoggingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Invoke_ShouldLogAtLeastTwice()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context);

            // Assert - At least a "Processing request" log and a "Response sent" log
            // (the enhanced middleware also logs the call-chain catalog, so the total
            // may be greater than 2).
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(2));
        }

        [Fact]
        public async Task Invoke_WhenNextThrows_ShouldPropagatException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var expectedException = new InvalidOperationException("Test exception");
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await _middleware.Invoke(context);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test exception");
        }

        [Fact]
        public async Task Invoke_ShouldInitialiseCallChainInHttpContextItems()
        {
            // Arrange
            var context = new DefaultHttpContext();
            List<string>? capturedChain = null;
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns<HttpContext>(ctx =>
            {
                capturedChain = ctx.Items[LoggingHelper.CallChainKey] as List<string>;
                return Task.CompletedTask;
            });

            // Act
            await _middleware.Invoke(context);

            // Assert — the call chain was initialised before next was called
            capturedChain.Should().NotBeNull("the middleware must initialise the call chain before calling next");
        }

        [Fact]
        public async Task Invoke_ShouldAddItselfToCallChain()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context);

            // Assert
            var chain = LoggingHelper.GetCallChain(context);
            chain.Should().Contain("LoggingMiddleware.Invoke",
                "the middleware must register itself in the call chain");
        }

        [Fact]
        public async Task Invoke_ShouldLogCallChainCatalog()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context);

            // Assert — the catalog log must contain the CALL-CHAIN marker
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CALL-CHAIN")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Invoke_WhenNextThrows_ShouldLogCallChainBeforeRethrow()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(new InvalidOperationException("boom"));

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.Invoke(context));

            // Assert — call chain catalog was still emitted despite the exception
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CALL-CHAIN")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Invoke_ShouldStoreTraceIdInHttpContextItems()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "test-trace-abc";
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context);

            // Assert — the trace/request ID was stored for downstream access
            context.Items[LoggingHelper.RequestIdKey].Should().Be("test-trace-abc");
        }

        [Fact]
        public async Task Invoke_TrackFunctionCall_DownstreamFunctionsAppearInChain()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns<HttpContext>(ctx =>
            {
                LoggingHelper.TrackFunctionCall(ctx, "SomeController.SomeAction");
                LoggingHelper.TrackFunctionCall(ctx, "SomeService.SomeMethod");
                return Task.CompletedTask;
            });

            // Act
            await _middleware.Invoke(context);

            // Assert
            var chain = LoggingHelper.GetCallChain(context);
            chain.Should().ContainInOrder(
                "LoggingMiddleware.Invoke",
                "SomeController.SomeAction",
                "SomeService.SomeMethod");
        }
    }
}
