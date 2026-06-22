using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.User;
using Microsoft.Extensions.Logging;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Code to unify logging practices throughout the application.
    /// </summary>
    public static class LoggingHelper
    {
        // HMAC-SHA256 key used to pseudonymise PII in logs.
        // Set once at startup via Initialize(); falls back to a random per-process key.
        private static byte[] _hmacKey = RandomNumberGenerator.GetBytes(32);

        /// <summary>
        /// Initialize the HMAC key used to hash PII values before they are written to logs.
        /// Call this once at application startup, passing a stable secret from configuration.
        /// If <paramref name="key"/> is null or empty a random key is generated for this
        /// process lifetime, which still protects PII but prevents cross-restart correlation.
        /// </summary>
        /// <param name="key">32-byte HMAC key. Supply from a secret store (Key Vault, User Secrets).</param>
        public static void Initialize(byte[]? key)
        {
            if (key != null && key.Length > 0)
            {
                if (key.Length != 32)
                {
                    // Use the provided key anyway (HMAC-SHA256 accepts any length), but warn
                    // that a 32-byte key is recommended for optimal security.
                    // A random 32-byte key is used as fallback when key is entirely missing.
                    // We do NOT fall back here so callers get consistent behaviour.
                }
                _hmacKey = key;
            }
            // else: keep the random key already assigned at field-initialization time.
        }

        /// <summary>
        /// Returns a short, stable HMAC-SHA256 hex token for a PII value.
        /// Identical inputs always produce identical tokens within the same process
        /// (or across processes when a stable key is configured), enabling log correlation
        /// without exposing the underlying value.
        /// </summary>
        internal static string HashPii(string? value)
        {
            byte[] inputBytes;
            byte[] hashBytes;
            string result;

            if (string.IsNullOrEmpty(value))
            {
                result = "(none)";
            }
            else
            {
                inputBytes = Encoding.UTF8.GetBytes(value);
                hashBytes = HMACSHA256.HashData(_hmacKey, inputBytes);
                // Return the first 12 bytes (96 bits) as hex — short enough to read in logs,
                // long enough to be collision-resistant for correlation purposes.
                result = Convert.ToHexString(hashBytes, 0, 12).ToLowerInvariant();
            }

            return result;
        }


        /// <summary>
        /// Utility function to log user activity.
        /// </summary>
        /// <param name="claimsPrincipal">User value from AspNetCore Razor Pages.</param>
        /// <param name="_logger">Transfer logging to function.</param>
        /// <param name="methodName">Which class.method calling the function. </param>
        /// <param name="noteRequestId">GUID distinctly identifying a request.</param>
        /// <returns>True if the user was fully identified in the claimsPrincipal for logging.</returns>
        public static async Task<bool> LogUserActivity(ClaimsPrincipal claimsPrincipal, ILogger _logger, string methodName, string noteRequestId, string? traceId = null)
        {

            bool answerIdentifiedUserFully = false;
            string userName;
            UserClaimsLoader userClaimsLoader;
            UserClaims userClaims;

            if (traceId != null)
            {
                LoggingHelper.LogTraceIdentifier(_logger, traceId);
            }

            if (claimsPrincipal.Identity != null)
            {

                userName = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "oid")?.Value ?? "Unknown User";
                LoggingHelper.LogDataProcessingStatusServiceWork(_logger, methodName, noteRequestId, DataProcessingStatus.Success, $"Auth for {HashPii(userName)}");

                try
                {
                    if (claimsPrincipal.Claims != null)
                    {
                        userClaimsLoader = new();
                        userClaims = await userClaimsLoader.LoadDataAsync(claimsPrincipal.Claims);

                        LoggingHelper.LogUserClaims(userClaims, _logger, methodName);
                        answerIdentifiedUserFully = true;
                    }
                    else
                    {
                        LoggingHelper.LogDataProcessingStatusServiceWork(_logger, methodName, noteRequestId, DataProcessingStatus.Info, $"User.Claims was null");
                    }
                }
                catch (Exception ex)
                {
                    LoggingHelper.LogDataProcessingStatusServiceWork(_logger, methodName, noteRequestId, DataProcessingStatus.Exception, $"{ex.Message}");
                }
            }
            else
            {
                userName = claimsPrincipal.Identity?.Name ?? "Unknown User, User.Identity not detected.";
                LoggingHelper.LogDataProcessingStatusServiceWork(_logger, methodName, noteRequestId, DataProcessingStatus.Failure, $"Failed Auth for {HashPii(userName)}");
            }

            return answerIdentifiedUserFully;
        }

        /// <summary>
        /// Method to log a string; expecting a trace identifier.
        /// </summary>
        /// <param name="_logger">logger</param>
        /// <param name="traceId">Expecting string from Activity.Current?.Id ?? HttpContext.TraceIdentifier</param>
        public static void LogTraceIdentifier(ILogger _logger, string traceId)
        {
            _logger.LogInformation("Trace Identifier: {0}", traceId);
        }


        public static void LogUserClaims(UserClaims userClaims, ILogger _logger, string methodName)
        {
            // Hash PII fields before logging to avoid storing plaintext personal data.
            // Tokens are consistent HMAC-SHA256 digests that support log correlation
            // without exposing the underlying Sid, Oid, email, or display name.
            string sidHash   = HashPii(userClaims.Sid);
            string oidHash   = HashPii(userClaims.Oid);
            string emailHash = HashPii(userClaims.Email);
            string nameHash  = HashPii(userClaims.Name);
            bool firstIterationComplete;

            // Log who is calling
            _logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}", DateTime.UtcNow, methodName, sidHash, oidHash, emailHash, nameHash);

            StringBuilder sb = new StringBuilder();
            if (userClaims.Roles != null)
            {
                firstIterationComplete = false;
                foreach (var role in userClaims.Roles)
                {
                    sb.Append(role);
                    if (firstIterationComplete)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        firstIterationComplete = true;
                    }
                }
            }

            _logger.LogInformation("{0} Oid carries the following permissions: {1}", oidHash, sb.ToString());


        }


        /// <summary>
        /// Method to log Requests and assoicate them with data processing in a certain method.
        /// </summary>
        /// <param name="request">Received HttpRequest</param>
        /// <param name="_logger">Logger used in the processing class.</param>
        /// <param name="methodName">Method doing the data processing.</param>
        /// <returns>GUID as string that identifies the Request in logs.</returns>
        public static string LogRequestReturnId(this HttpRequest request, ILogger _logger, string methodName)
        {

            string noteRequestId = Guid.NewGuid().ToString();
            StringValues origin;
            StringValues host;

            // Log who is calling
            // In this scheme, we do not see Origin headers being sent from the requestor.
            request.Headers.TryGetValue("Origin", out origin);
            request.Headers.TryGetValue("Host", out host);
            if (
                !string.IsNullOrEmpty(origin) || !string.IsNullOrEmpty(host)
                )
            {
                _logger.LogInformation("{0} {1} called from Origin {2} Host {3} associated with {4}", DateTime.UtcNow, methodName, origin, host, noteRequestId);
            }
            else
            {
                _logger.LogInformation("{0} {1} called from unknown Origin and Host associated with {2}", DateTime.UtcNow, methodName, noteRequestId);
            }

            return noteRequestId;

        }


        /// <summary>
        /// Method to log Requests and assoicate them with data processing in a certain method.
        /// Uses Reflection to determine class and function which called it.
        /// If called from an async/await, it will yield the compiler-derived class name instead of the human written one.  
        /// </summary>
        /// <param name="request">Received HttpRequest</param>
        /// <param name="_logger">Logger used in the processing class.</param>
        /// <returns>GUID as string that identifies the Request in logs.</returns>
        [Obsolete("Did not obtain practical benefit from this version due to compiler-derived names.")]
        public static string LogRequestReturnId(this HttpRequest request, ILogger _logger)
        {
            // Distinctly identify a Requst in the logs.
            string noteRequestId = Guid.NewGuid().ToString();
            string methodName;
            StringValues origin;
            StringValues host;
            MethodBase? callingMethod;
            Type? callingClass;

            // Be able to name the calling class and method in logs
            StringBuilder sbCallingMethod = new StringBuilder();
            StringBuilder sbCallingClass = new StringBuilder();
            StringBuilder sbMethodName = new StringBuilder();

            // Use Reflection to identify which class and method called this class and method.
            StackTrace st = new StackTrace();
            StackFrame? frame = st.GetFrame(1);
            if (frame != null)
            {
                callingMethod = frame.GetMethod();
                if (callingMethod != null)
                {
                    callingClass = callingMethod.DeclaringType;


                    sbCallingMethod.Append(callingMethod.Name);
                    if (callingClass != null)
                    {
                        if (callingClass.Name.Contains("<"))
                        {
                            sbCallingClass.Append(callingClass.Name.Substring(0, callingClass.Name.IndexOf("<")));
                        }
                        else
                        {
                            sbCallingClass.Append(callingClass.Name);
                        }


                    }
                    else
                    {
                        sbCallingClass.Append("undetermined Class");
                    }
                }
                else
                {
                    sbCallingMethod.Append("undetermined Method");
                }

                sbMethodName.Append(sbCallingClass.ToString());
                sbMethodName.Append('.');
                sbMethodName.Append(sbCallingMethod.ToString());
            }

            methodName = sbMethodName.ToString();


            // Log who is calling
            // In this scheme, we do not see Origin headers being sent from the requestor.
            request.Headers.TryGetValue("Origin", out origin);
            request.Headers.TryGetValue("Host", out host);
            if (
                !string.IsNullOrEmpty(origin) || !string.IsNullOrEmpty(host)
                )
            {
                _logger.LogInformation("{0} {1} called from Origin {2} Host {3} associated with {4}", DateTime.UtcNow, methodName, origin, host, noteRequestId);
            }
            else
            {
                _logger.LogInformation("{0} {1} called from unknown Origin and Host associated with {2}", DateTime.UtcNow, methodName, noteRequestId);
            }

            return noteRequestId;

        }

        /// <summary>
        /// Use StackTrace and Reflection to get a class and method name.
        /// Frequently returning values like ".MoveNext" due to ASP NET CORE Kestrel patterns.
        /// </summary>
        /// <returns>Name of class.method</returns>
        public static string IdentifyCallingClassAndMethod()
        {
            MethodBase? callingMethod;
            Type? callingClass;

            // Be able to name the calling class and method in logs
            StringBuilder sbCallingMethod = new StringBuilder();
            StringBuilder sbCallingClass = new StringBuilder();
            StringBuilder sbMethodName = new StringBuilder();

            // Use Reflection to identify which class and method called this class and method.
            StackTrace st = new StackTrace();
            StackFrame? frame = st.GetFrame(1);
            if (frame != null)
            {
                callingMethod = frame.GetMethod();
                if (callingMethod != null)
                {
                    callingClass = callingMethod.DeclaringType;


                    sbCallingMethod.Append(callingMethod.Name);
                    if (callingClass != null)
                    {
                        if (callingClass.Name.Contains("<"))
                        {
                            sbCallingClass.Append(callingClass.Name.Substring(0, callingClass.Name.IndexOf("<")));
                        }
                        else
                        {
                            sbCallingClass.Append(callingClass.Name);
                        }


                    }
                    else
                    {
                        sbCallingClass.Append("undetermined Class");
                    }
                }
                else
                {
                    sbCallingMethod.Append("undetermined Method");
                }

                sbMethodName.Append(sbCallingClass.ToString());
                sbMethodName.Append('.');
                sbMethodName.Append(sbCallingMethod.ToString());
            }

            return sbMethodName.ToString();
        }


        /// <summary>
        /// Code to log the status of data processing in the application.
        /// </summary>
        /// <param name="request">HTTP Request object of the initially received API request.</param>
        /// <param name="_logger">Logger used by the class.</param>
        /// <param name="methodName">Function from which the logger should be called.</param>
        /// <param name="noteRequestId">GUID to distinctly identify the Note to be acted upon.</param>
        /// <param name="status">Success, Failure, or other DataProcessingStatus</param>
        /// <param name="cause">String describing the circumstances of success, failure, or other logged event.</param>
        /// <returns>noteRequestId that was logged.</returns>
        public static string LogDataProcessingStatusRequest(this HttpRequest request, ILogger _logger, string? methodName, string noteRequestId, DataProcessingStatus status, string cause)
        {

            _logger.LogInformation("{0} {1} request {2} {3} due to {4}.", DateTime.UtcNow, methodName, noteRequestId, status, cause);

            return noteRequestId;

        }

        /// <summary>
        /// Code to log the status of a service's work in the application.  Assumes no direct HTTP REQUEST to the function associated with this logging.
        /// </summary>
        /// <param name="_logger">Logger used by the class.</param>
        /// <param name="methodName">Function from which the logger should be called.</param>
        /// <param name="noteRequestId">GUID to distinctly identify the Note to be acted upon.</param>
        /// <param name="status">Success, Failure, or other DataProcessingStatus</param>
        /// <param name="cause">String describing the circumstances of success, failure, or other logged event.</param>
        /// <returns>noteRequestId that was logged.</returns>
        public static string LogDataProcessingStatusServiceWork(ILogger _logger, string? methodName, string noteRequestId, DataProcessingStatus status, string cause)
        {

            _logger.LogInformation("{0} {1} request {2} {3} due to {4}.", DateTime.UtcNow, methodName, noteRequestId, status, cause);

            return noteRequestId;

        }

        /// <summary>
        /// Code to log the status of a service's work in the application.  Assumes no direct HTTP REQUEST to the function associated with this logging.
        /// </summary>
        /// <param name="_logger">Logger used by the class.</param>
        /// <param name="noteRequestId">GUID to distinctly identify the Note to be acted upon.</param>
        /// <param name="status">Success, Failure, or other DataProcessingStatus</param>
        /// <param name="cause">String describing the circumstances of success, failure, or other logged event.</param>
        /// <returns>noteRequestId that was logged.</returns>
        public static string LogDataProcessingStatusServiceWork(ILogger _logger, string noteRequestId, DataProcessingStatus status, string cause)
        {

            _logger.LogInformation("{0} request {1} {2} due to {3}.", DateTime.UtcNow, noteRequestId, status, cause);

            return noteRequestId;

        }

        /// <summary>
        /// Overload without noteRequestId - logs with timestamp
        /// </summary>
        public static void LogDataProcessingStatusServiceWorkSimple(
            ILogger logger,
            string caller,
            DataProcessingStatus status,
            string message)
        {
            logger.LogInformation("{0} {1} {2} - {3}", DateTime.UtcNow, caller, status, message);
        }

        /// <summary>
        /// Overload with context parameter - logs with timestamp and contextual information
        /// </summary>
        public static void LogDataProcessingStatusWithContext(
            ILogger logger,
            string caller,
            string context,
            DataProcessingStatus status,
            string message)
        {
            var contextInfo = string.IsNullOrEmpty(context) ? string.Empty : $"[{context}] ";
            logger.LogInformation("{0} {1} {2}{3} - {4}", DateTime.UtcNow, caller, contextInfo, status, message);
        }


        // ------------------------------------------------------------------ Call-chain tracking

        /// <summary>
        /// Key used to store the per-request call chain in <see cref="HttpContext.Items"/>.
        /// </summary>
        public const string CallChainKey = "LoggingHelper_CallChain";

        /// <summary>
        /// Key used to store the per-request trace identifier in <see cref="HttpContext.Items"/>
        /// so downstream components can reference it without needing the full HttpContext.
        /// </summary>
        public const string RequestIdKey = "LoggingHelper_RequestId";

        /// <summary>
        /// Appends <paramref name="functionName"/> to the per-request call chain stored in
        /// <see cref="HttpContext.Items"/>.  Call this at the start of any method that should
        /// appear in the logged call chain.  Does nothing if <paramref name="context"/> is null
        /// or the chain has not been initialised by the logging middleware.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="functionName">Human-readable "ClassName.MethodName" label.</param>
        public static void TrackFunctionCall(HttpContext? context, string functionName)
        {
            if (context?.Items[CallChainKey] is List<string> chain)
            {
                chain.Add(functionName);
            }
        }

        /// <summary>
        /// Returns the accumulated call chain for the current request, or an empty list
        /// if tracking has not been initialised.
        /// </summary>
        public static IReadOnlyList<string> GetCallChain(HttpContext? context)
        {
            if (context?.Items[CallChainKey] is List<string> chain)
            {
                return chain;
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Writes the call chain to the log in catalog style, aligned with the user session
        /// and the current HTTP request.
        /// </summary>
        /// <param name="logger">Logger for the middleware or component writing the summary.</param>
        /// <param name="traceId">ASP.NET Core <c>HttpContext.TraceIdentifier</c>.</param>
        /// <param name="sessionId">Hashed user session identifier (SID claim or ASP.NET session ID).</param>
        /// <param name="outcome">Short outcome label, e.g. "Response 200", "Exception InvalidOperationException", "Aborted".</param>
        /// <param name="chain">Ordered list of function names activated during the request.</param>
        public static void LogCallChainCatalog(
            ILogger logger,
            string traceId,
            string sessionId,
            string outcome,
            IReadOnlyList<string> chain)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"  ┌─ CALL-CHAIN | {DateTime.UtcNow:u} | Trace {traceId} | Session {sessionId} | {outcome}");

            if (chain.Count == 0)
            {
                sb.AppendLine("  │   (no functions tracked)");
            }
            else
            {
                for (int i = 0; i < chain.Count; i++)
                {
                    string prefix = (i == chain.Count - 1) ? "  └─" : "  ├─";
                    sb.AppendLine($"  {prefix} {i + 1,2}. {chain[i]}");
                }
            }

            logger.LogInformation("{CallChainCatalog}", sb.ToString().TrimEnd());
        }
    }
}
