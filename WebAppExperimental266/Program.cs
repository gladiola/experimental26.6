using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebAppExperimental266.Models.Main_Objects;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Extensions;
using WebAppExperimental266.Services;

namespace WebAppExperimental266
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var environment = builder.Environment;

            // Logging
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger("Program");
            logger.LogInformation("=== Application Starting - {Environment} ===", environment.EnvironmentName);

            // Initialize PII hashing for log output (security task 16).
            // Supply a stable 32-byte base64 key via Logging:PiiHmacKey (User Secrets / Key Vault).
            // If absent or invalid, a random key is used for this process lifetime.
            var piiHmacKeyBase64 = builder.Configuration["Logging:PiiHmacKey"];
            byte[]? piiHmacKey = null;
            if (!string.IsNullOrWhiteSpace(piiHmacKeyBase64))
            {
                try
                {
                    var decoded = Convert.FromBase64String(piiHmacKeyBase64);
                    if (decoded.Length == 32)
                    {
                        piiHmacKey = decoded;
                    }
                    else
                    {
                        logger.LogWarning("Logging:PiiHmacKey must be exactly 32 bytes when base64-decoded (got {Length} bytes); a random key will be used for this session.", decoded.Length);
                    }
                }
                catch (FormatException)
                {
                    logger.LogWarning("Logging:PiiHmacKey is not valid base64; a random key will be used for this session.");
                }
            }
            LoggingHelper.Initialize(piiHmacKey);

            // Load feature flags
            builder.Services.AddFeatureFlags(builder.Configuration);
            var featureFlags = builder.Configuration.GetSection("FeatureFlags").Get<FeatureFlags>() ?? new FeatureFlags();

            logger.LogInformation("Feature Flags Loaded: AzureAd={AzureAd}, CosmosDb={CosmosDb}, BlobStorage={BlobStorage}, AwsSecretsManager={AwsSM}, AwsDynamoDb={AwsDynamo}, AwsCognito={AwsCognito}, GcpSecretManager={GcpSM}, GcpFirestore={GcpFirestore}, GcpIdentity={GcpIdentity}",
                featureFlags.EnableAzureAd, featureFlags.EnableCosmosDb, featureFlags.EnableBlobStorage,
                featureFlags.EnableAwsSecretsManager, featureFlags.EnableAwsDynamoDb, featureFlags.EnableAwsCognito,
                featureFlags.EnableGcpSecretManager, featureFlags.EnableGcpFirestore, featureFlags.EnableGcpIdentity);

            // Core services
            builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();

            // Phase 1: Basic Infrastructure
            builder.Services.AddSessionConfiguration(logger, featureFlags.EnableSession);
            builder.Services.AddLocalizationConfiguration(logger, featureFlags.EnableLocalization);

            // Phase 2: Authentication & Authorization
            builder.Services.AddAzureAdAuthentication(
                builder.Configuration,
                logger,
                featureFlags.EnableAzureAd);

            // Phase 2.5: mTLS Client Certificate Authentication (if enabled)
            if (featureFlags.EnableMtls)
            {
                builder.Services.AddMtlsAuthentication(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableMtls);
            }

            // Phase 2.5: YubiKey PIV MFA (application-level second factor)
            if (featureFlags.EnableYubiKeyRequired)
            {
                builder.Services.AddYubiKeyMfaServices(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableYubiKeyRequired);
            }

            builder.Services.AddRazorPagesConfiguration(
                logger,
                featureFlags.EnableAuthorization,
                featureFlags.EnableYubiKeyRequired);
            builder.Services.AddCrudDataServices(builder.Configuration, logger, environment);
            builder.Services.AddAdminCertificateAuthorization(builder.Configuration, logger);

            // Phase 3: Azure Services (Advanced)
            if (featureFlags.EnableKeyVault)
            {
                await builder.Services.AddKeyVaultServicesAsync(
                    builder.Configuration,
                    loggerFactory,
                    environment,
                    builder);
            }

            // Phase 2: Security (Nonce & CSP)
            builder.Services.AddNonceServices(
                builder.Configuration,
                logger,
                featureFlags.EnableNonceServices);

            // Phase 3: Data Storage (Optional)
            if (featureFlags.EnableBlobStorage)
            {
                builder.Services.AddBlobStorageServices(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableBlobStorage);
            }

            if (featureFlags.EnableCosmosDb)
            {
                await builder.Services.AddCosmosDbServicesAsync(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableCosmosDb);
            }

            // Phase 3: AWS Cloud Services (Optional)
            if (featureFlags.EnableAwsSecretsManager)
            {
                builder.Services.AddAwsSecretsManagerServices(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableAwsSecretsManager);
            }

            if (featureFlags.EnableAwsDynamoDb)
            {
                await builder.Services.AddAwsDynamoDbServicesAsync(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableAwsDynamoDb);
            }

            // Phase 3: GCP Cloud Services (Optional)
            if (featureFlags.EnableGcpSecretManager)
            {
                builder.Services.AddGcpSecretManagerServices(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableGcpSecretManager);
            }

            if (featureFlags.EnableGcpFirestore)
            {
                await builder.Services.AddGcpFirestoreServicesAsync(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableGcpFirestore);
            }

            // Phase 2: AWS Cognito Identity Management (Optional)
            if (featureFlags.EnableAwsCognito)
            {
                builder.Services.AddAwsCognitoAuthentication(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableAwsCognito);
            }

            // Phase 2: GCP Identity Platform (Optional)
            if (featureFlags.EnableGcpIdentity)
            {
                builder.Services.AddGcpIdentityAuthentication(
                    builder.Configuration,
                    logger,
                    featureFlags.EnableGcpIdentity);
            }

            logger.LogInformation("=== Service Configuration Complete ===");

            // Build app
            var app = builder.Build();
            await app.EnsureCrudDataStoreCreatedAsync(logger);

            // Configure HTTP pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();

            // Localization — must be before UseRouting so culture is set for all middleware
            app.UseLocalizationConfiguration(logger, featureFlags.EnableLocalization);

            // Security Middleware — must be first so every response (including 401/403
            // short-circuits from auth) carries the security headers.
            if (featureFlags.EnableCSP && featureFlags.EnableNonceServices)
            {
                await app.UseNonceAndSecurityHeadersAsync(logger, enabled: true);
            }

            if (featureFlags.EnableSecurityHeaders)
            {
                app.UseStandardSecurityHeaders(logger, enabled: true);
            }

            app.UseRouting();

            if (featureFlags.EnableSession)
            {
                app.UseSession();
            }

            if (featureFlags.EnableAzureAd ||
                featureFlags.EnableAwsCognito ||
                featureFlags.EnableGcpIdentity ||
                featureFlags.EnableMtls)
            {
                app.UseAuthentication();
            }

            // UseAuthorization must always be present when any endpoint has authorization metadata
            app.UseAuthorization();

            // Map endpoints
            app.MapStaticAssets();
            app.MapRazorPages().WithStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            logger.LogInformation("=== Application Ready - Starting Server ===");
            app.Run();
        }
    }
}