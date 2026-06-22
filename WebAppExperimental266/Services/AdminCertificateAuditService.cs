using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace WebAppExperimental266.Services
{
    public interface IAdminCertificateAuditService
    {
        void LogAuthenticationAttempt(HttpContext? httpContext, ClaimsPrincipal user, bool success, string detail);

        void LogPageAccess(HttpContext? httpContext, ClaimsPrincipal user, string pageName);
    }

    public class AdminCertificateAuditService : IAdminCertificateAuditService
    {
        private readonly ILogger<AdminCertificateAuditService> _logger;

        public AdminCertificateAuditService(ILogger<AdminCertificateAuditService> logger)
        {
            _logger = logger;
        }

        public void LogAuthenticationAttempt(HttpContext? httpContext, ClaimsPrincipal user, bool success, string detail)
        {
            var certificate = httpContext?.Connection.ClientCertificate;
            _logger.LogInformation(
                "Admin certificate authentication {Outcome} | Trace {TraceId} | User {UserId} | Path {Path} | Thumbprint {Thumbprint} | Subject {SubjectHash} | Detail {Detail}",
                success ? "succeeded" : "failed",
                httpContext?.TraceIdentifier ?? "(no-trace)",
                LoggingHelper.HashPii(UserIdentityHelper.GetStableUserId(user)),
                SanitizePath(httpContext?.Request.Path.Value),
                GetThumbprint(certificate),
                LoggingHelper.HashPii(certificate?.Subject),
                detail);
        }

        public void LogPageAccess(HttpContext? httpContext, ClaimsPrincipal user, string pageName)
        {
            var certificate = httpContext?.Connection.ClientCertificate;
            _logger.LogInformation(
                "Admin page access | Trace {TraceId} | User {UserId} | Page {PageName} | Path {Path} | Thumbprint {Thumbprint} | Subject {SubjectHash}",
                httpContext?.TraceIdentifier ?? "(no-trace)",
                LoggingHelper.HashPii(UserIdentityHelper.GetStableUserId(user)),
                pageName,
                SanitizePath(httpContext?.Request.Path.Value),
                GetThumbprint(certificate),
                LoggingHelper.HashPii(certificate?.Subject));
        }

        private static string GetThumbprint(X509Certificate2? certificate)
        {
            return certificate?.Thumbprint is null
                ? "(none)"
                : Models.Settings.AdminCertificateSettings.NormalizeThumbprint(certificate.Thumbprint);
        }

        private static string SanitizePath(string? path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? "/"
                : path.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }
    }
}
