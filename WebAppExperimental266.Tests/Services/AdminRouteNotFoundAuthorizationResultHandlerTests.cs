using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AdminRouteNotFoundAuthorizationResultHandlerTests
    {
        [Fact]
        public async Task HandleAsync_Returns404_ForForbiddenAdminCertificateRoute()
        {
            var handler = new AdminRouteNotFoundAuthorizationResultHandler();
            var context = BuildHttpContext(new AuthorizeAttribute { Policy = "AdminCertificate" });
            var nextCalled = false;
            Task Next(HttpContext _) { nextCalled = true; return Task.CompletedTask; }

            await handler.HandleAsync(
                Next,
                context,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Forbid());

            context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_Delegates_ForForbiddenNonAdminRoute()
        {
            var handler = new AdminRouteNotFoundAuthorizationResultHandler();
            var context = BuildHttpContext(new AuthorizeAttribute { Policy = "OtherPolicy" });
            var authService = new Mock<IAuthenticationService>();
            authService
                .Setup(service => service.ForbidAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string?>(),
                    It.IsAny<AuthenticationProperties?>()))
                .Returns<HttpContext, string?, AuthenticationProperties?>((httpContext, _, _) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                });
            context.RequestServices = new ServiceCollection()
                .AddSingleton(authService.Object)
                .BuildServiceProvider();

            await handler.HandleAsync(
                _ => Task.CompletedTask,
                context,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Forbid());

            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        private static DefaultHttpContext BuildHttpContext(IAuthorizeData authorizeData)
        {
            var context = new DefaultHttpContext();
            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(authorizeData),
                "test-endpoint");
            context.SetEndpoint(endpoint);
            return context;
        }
    }
}
