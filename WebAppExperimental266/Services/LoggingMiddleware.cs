using System.Security.Claims;
using WebAppExperimental266.Models.Main_Objects;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Middleware that associates every HTTP request with a user session and records the
    /// ordered chain of functions activated to produce the response.  At the end of each
    /// request the full chain is written to the log in catalog style together with the
    /// session identifier and outcome (success, exception, or client abort).
    /// </summary>
    public class LoggingMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            string caller = "LoggingMiddleware.Invoke";

            // Initialise the per-request call chain so downstream components can register.
            context.Items[LoggingHelper.CallChainKey] = new List<string>();

            // Use the built-in trace identifier as the request correlation key.
            string traceId = context.TraceIdentifier;
            context.Items[LoggingHelper.RequestIdKey] = traceId;

            string method = context.Request.Method;
            string path = context.Request.Path;

            // Track this middleware in the call chain.
            LoggingHelper.TrackFunctionCall(context, caller);

            _logger.LogInformation(
                "Processing request | {Method} {Path} | Trace {TraceId} | {Time}",
                method, path, traceId, DateTime.UtcNow);

            bool chainLogged = false;
            try
            {
                await _next(context);

                // After the full pipeline has run, authentication claims are populated.
                string sessionId = ResolveSessionId(context);
                string outcome = $"Response {context.Response.StatusCode}";

                LoggingHelper.LogCallChainCatalog(
                    _logger,
                    traceId,
                    sessionId,
                    outcome,
                    LoggingHelper.GetCallChain(context));

                chainLogged = true;

                _logger.LogInformation(
                    "Response sent | {Method} {Path} | Trace {TraceId} | Session {SessionId} | Status {StatusCode} | {Time}",
                    method, path, traceId, sessionId, context.Response.StatusCode, DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                string sessionId = ResolveSessionId(context);

                LoggingHelper.LogCallChainCatalog(
                    _logger,
                    traceId,
                    sessionId,
                    "Aborted",
                    LoggingHelper.GetCallChain(context));

                chainLogged = true;

                LoggingHelper.LogDataProcessingStatusServiceWork(
                    _logger, caller, traceId, DataProcessingStatus.Warning,
                    $"Request aborted by client | {method} {path}");

                throw;
            }
            catch (Exception ex)
            {
                string sessionId = ResolveSessionId(context);

                LoggingHelper.LogCallChainCatalog(
                    _logger,
                    traceId,
                    sessionId,
                    $"Exception {ex.GetType().Name}",
                    LoggingHelper.GetCallChain(context));

                chainLogged = true;

                LoggingHelper.LogDataProcessingStatusServiceWork(
                    _logger, caller, traceId, DataProcessingStatus.Exception,
                    $"{ex.Message} | {method} {path}");

                throw;
            }
            finally
            {
                // Safety net: if the pipeline was interrupted in an unexpected way and we
                // have not yet logged the chain, emit it now so the trace is never lost.
                if (!chainLogged)
                {
                    string sessionId = ResolveSessionId(context);

                    LoggingHelper.LogCallChainCatalog(
                        _logger,
                        traceId,
                        sessionId,
                        "Interrupted",
                        LoggingHelper.GetCallChain(context));
                }
            }
        }

        /// <summary>
        /// Derives a privacy-safe session identifier from the authenticated user's SID claim
        /// (preferred) or falls back to the ASP.NET Core session ID and then to the trace
        /// identifier.  PII values are HMAC-hashed before being written to the log.
        /// </summary>
        private static string ResolveSessionId(HttpContext context)
        {
            string? sidClaim = context.User?.FindFirstValue("sid");
            if (!string.IsNullOrEmpty(sidClaim))
            {
                return LoggingHelper.HashPii(sidClaim);
            }

            // Avoid accessing Session.Id when the session feature may not be available;
            // ISession throws if the session provider is not configured.
            try
            {
                string? sessionId = context.Session?.Id;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    return LoggingHelper.HashPii(sessionId);
                }
            }
            catch (InvalidOperationException)
            {
                // Session provider not registered — fall through to trace identifier.
            }

            return context.TraceIdentifier;
        }
    }
}
