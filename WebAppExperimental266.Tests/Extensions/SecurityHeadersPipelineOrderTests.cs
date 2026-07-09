using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Extensions;

namespace WebAppExperimental266.Tests.Extensions
{
    /// <summary>
    /// Tests verifying that security headers are emitted before auth middleware can short-circuit
    /// the request pipeline — security fix #9.
    /// </summary>
    public class SecurityHeadersPipelineOrderTests
    {
        private static IApplicationBuilder CreateAppBuilder()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            return new ApplicationBuilder(services);
        }

        /// <summary>
        /// Security fix #9: verifies that the standard security headers middleware adds headers
        /// to the response even when the next middleware terminates the pipeline immediately
        /// (simulating a 401 / 403 short-circuit from auth middleware).
        /// This test fails if the fix is undone and security headers are placed after routing/auth.
        /// </summary>
        [Fact]
        public async Task SecurityHeaders_ArePresent_WhenNextMiddlewareShortCircuits()
        {
            // Arrange — security headers middleware first, then a terminal middleware that
            // represents an auth rejection without calling any downstream delegate.
            var mockLogger = new Mock<ILogger>();
            var appBuilder = CreateAppBuilder();

            appBuilder.UseStandardSecurityHeaders(mockLogger.Object, enabled: true);
            appBuilder.Run(async ctx =>
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized");
            });

            var app = appBuilder.Build();
            var context = new DefaultHttpContext();
            context.Response.Body = new System.IO.MemoryStream();

            // Act
            await app(context);

            // Assert — all standard security headers must be present even on a short-circuit response
            context.Response.Headers.Should().ContainKey("X-Frame-Options",
                "security headers must be applied before the pipeline can short-circuit (security fix #9)");
            context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
            context.Response.Headers.Should().ContainKey("Referrer-Policy");
        }

        /// <summary>
        /// Confirms that when security headers middleware is placed AFTER a short-circuiting
        /// middleware, the headers are absent — illustrating why placement matters and what
        /// fix #9 corrects.
        /// </summary>
        [Fact]
        public async Task SecurityHeaders_AreAbsent_WhenMiddlewareIsAfterShortCircuit()
        {
            // Arrange — short-circuit middleware first (the BUG scenario / incorrect order)
            var mockLogger = new Mock<ILogger>();
            var appBuilder = CreateAppBuilder();

            appBuilder.Run(async ctx =>
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized");
            });
            // Security headers registered after — they will never run
            appBuilder.UseStandardSecurityHeaders(mockLogger.Object, enabled: true);

            var app = appBuilder.Build();
            var context = new DefaultHttpContext();
            context.Response.Body = new System.IO.MemoryStream();

            // Act
            await app(context);

            // Assert — headers must NOT be present (documenting the vulnerability)
            context.Response.Headers.Should().NotContainKey("X-Frame-Options",
                "placing security headers AFTER a short-circuit loses the headers on that response");
        }

        [Fact]
        public async Task UseStandardSecurityHeaders_SetsAllExpectedHeaders()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var appBuilder = CreateAppBuilder();
            appBuilder.UseStandardSecurityHeaders(mockLogger.Object, enabled: true);
            appBuilder.Run(_ => Task.CompletedTask);

            var app = appBuilder.Build();
            var context = new DefaultHttpContext();
            context.Response.Body = new System.IO.MemoryStream();

            // Act
            await app(context);

            // Assert
            context.Response.Headers.Should().ContainKey("X-Frame-Options");
            context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
            context.Response.Headers.Should().ContainKey("X-XSS-Protection");
            context.Response.Headers.Should().ContainKey("Referrer-Policy");
            context.Response.Headers.Should().ContainKey("Cross-Origin-Opener-Policy");
            context.Response.Headers.Should().ContainKey("Cache-Control");
        }

        [Fact]
        public async Task UseStandardSecurityHeaders_DoesNotSetNoStoreHeaders_ForStaticAssets()
        {
            var mockLogger = new Mock<ILogger>();
            var appBuilder = CreateAppBuilder();
            appBuilder.UseStandardSecurityHeaders(mockLogger.Object, enabled: true);
            appBuilder.Run(_ => Task.CompletedTask);

            var app = appBuilder.Build();
            var context = new DefaultHttpContext();
            context.Request.Path = "/js/site.js";
            context.Response.Body = new System.IO.MemoryStream();

            await app(context);

            context.Response.Headers.Should().NotContainKey("Cache-Control");
            context.Response.Headers.Should().NotContainKey("Pragma");
            context.Response.Headers.Should().NotContainKey("Expires");
        }

        [Fact]
        public async Task UseStandardSecurityHeaders_WhenDisabled_SetsNoHeaders()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var appBuilder = CreateAppBuilder();
            appBuilder.UseStandardSecurityHeaders(mockLogger.Object, enabled: false);
            appBuilder.Run(_ => Task.CompletedTask);

            var app = appBuilder.Build();
            var context = new DefaultHttpContext();
            context.Response.Body = new System.IO.MemoryStream();

            // Act
            await app(context);

            // Assert
            context.Response.Headers.Should().NotContainKey("X-Frame-Options");
        }
    }
}
