using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace WebAppExperimental266.Services
{
    public class AdminRouteNotFoundAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private static readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden && IsAdminCertificateEndpoint(context.GetEndpoint()))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await DefaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        private static bool IsAdminCertificateEndpoint(Endpoint? endpoint)
        {
            if (endpoint == null)
            {
                return false;
            }

            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            return authorizeData.Any(metadata =>
                string.Equals(metadata.Policy, "AdminCertificate", StringComparison.Ordinal));
        }
    }
}
