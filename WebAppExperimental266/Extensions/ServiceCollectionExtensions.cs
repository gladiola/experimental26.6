using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using WebAppExperimental266.AwsSecretManager;
using WebAppExperimental266.GcpSecretManager;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;
using WebAppExperimental266.AzureKeyVaultOperations;
using WebAppExperimental266.Interfaces.Main_Objects;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;

namespace WebAppExperimental266.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure feature flags from appsettings
        /// </summary>
        public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FeatureFlags>(configuration.GetSection("FeatureFlags"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<FeatureFlags>>().Value);
            return services;
        }

        /// <summary>
        /// Add session and distributed cache
        /// </summary>
        public static IServiceCollection AddSessionConfiguration(this IServiceCollection services, ILogger logger, bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Session is DISABLED");
            }
            else
            {
                services.AddDistributedMemoryCache();
                services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                });

                logger.LogInformation("Session configured with 30-minute timeout");
            }

            return services;
        }

        /// <summary>
        /// Configure localization for en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA,
        /// sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, and ga-IE
        /// </summary>
        public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services, ILogger logger, bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Localization is DISABLED");
            }
            else
            {
                services.AddLocalization(options => options.ResourcesPath = "Resources");

                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("de-DE"),
                    new CultureInfo("es-ES"),
                    new CultureInfo("fr-FR"),
                    new CultureInfo("pt-PT"),
                    new CultureInfo("it-IT"),
                    new CultureInfo("zh-HK"),
                    new CultureInfo("ko-KR"),
                    new CultureInfo("hi-IN"),
                    new CultureInfo("ru-RU"),
                    new CultureInfo("ar-SA"),
                    new CultureInfo("sw-KE"),
                    new CultureInfo("ja-JP"),
                    new CultureInfo("ht-HT"),
                    new CultureInfo("haw-US"),
                    new CultureInfo("sm-WS"),
                    new CultureInfo("mi-NZ"),
                    new CultureInfo("af-ZA"),
                    new CultureInfo("nl-NL"),
                    new CultureInfo("ha-NG"),
                    new CultureInfo("am-ET"),
                    new CultureInfo("yo-NG"),
                    new CultureInfo("bn-BD"),
                    new CultureInfo("zh-CN"),
                    new CultureInfo("ga-IE"),
                };

                services.Configure<RequestLocalizationOptions>(options =>
                {
                    options.DefaultRequestCulture = new RequestCulture("en-US");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                });

                logger.LogInformation("Localization configured for en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ga-IE");
            }

            return services;
        }

        /// <summary>
        /// Configure Azure AD authentication and authorization
        /// </summary>
        public static IServiceCollection AddAzureAdAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Azure AD Authentication is DISABLED");
            }
            else
            {
                var adSettings = configuration.GetSection("AzureAD").Get<AzureADSettings>()
                    ?? throw new InvalidOperationException("AzureADSettings not found");

                logger.LogInformation("Configuring Azure AD with ClientId: {ClientId}", adSettings.ClientId);

                services.AddSingleton<IAzureADSettingsService>(new AzureADSettingsService(adSettings));

                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));

                services.ConfigureApplicationCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        logger.LogError("Redirect to login for path: {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    };
                });

                services.AddAuthorization();
            }

            return services;
        }

        /// <summary>
        /// Configure Razor Pages with authorization policies
        /// </summary>
        public static IServiceCollection AddRazorPagesConfiguration(
            this IServiceCollection services,
            ILogger logger,
            bool enableAuthorization = true)
        {
            services.AddControllersWithViews();

            var razorPages = services.AddRazorPages(options =>
            {
                if (enableAuthorization)
                {
                    options.Conventions.AuthorizeFolder("/Experimental");

                }

                options.Conventions.AllowAnonymousToPage("/Privacy");
                options.Conventions.AllowAnonymousToPage("/Error");
                options.Conventions.AllowAnonymousToPage("/About");
            })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();

            if (enableAuthorization)
            {
                razorPages.AddMicrosoftIdentityUI();
                logger.LogInformation("Razor Pages configured WITH authorization and view localization");
            }
            else
            {
                logger.LogInformation("Razor Pages configured WITHOUT authorization, with view localization");
            }

            return services;
        }

        /// <summary>
        /// Configure Azure Key Vault operations
        /// </summary>
        public static async Task<IServiceCollection> AddKeyVaultServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment environment,
            WebApplicationBuilder builder,
            bool enabled = true)
        {
            if (!enabled)
            {
                loggerFactory.CreateLogger("KeyVault").LogWarning("Key Vault integration is DISABLED");
            }
            else
            {
                var logger = loggerFactory.CreateLogger("KeyVault");

                try
                {
                    var kvSettings = configuration.GetSection("AzureKeyVault").Get<KeyVaultSettings>()
                        ?? throw new InvalidOperationException("KeyVaultSettings not found");

                    var adSettings = configuration.GetSection("AzureAD").Get<AzureADSettings>()
                        ?? throw new InvalidOperationException("AzureADSettings not found for Key Vault");

                    services.AddSingleton<IKeyVaultSettingsService>(new KeyVaultSettingsService(kvSettings));
                    services.AddSingleton<IAzureKeyVaultCertificateOperations, AzureKeyVaultCertificateOperations>();
                    services.AddSingleton<IAzureKeyVaultOperationsService, AzureKeyVaultOperationsService>();

                    // Retrieve certificate
                    var akvo = new AzureKeyVaultCertificateOperations(loggerFactory.CreateLogger<AzureKeyVaultCertificateOperations>());
                    
                    var clientSecret = adSettings.ClientCredentials.FirstOrDefault(cc => cc.SourceType == "ClientSecret")?.ClientSecret ?? string.Empty;
                    
                    var serverCert = await akvo.GetCertificateFromKeyVault(
                        adSettings.TenantId,
                        adSettings.ClientId,
                        kvSettings.KeyVaultURL,
                        kvSettings.KeyVaultSecret,
                        kvSettings.KeyVaultPassName);

                    if (serverCert == null)
                    {
                        throw new InvalidOperationException("Server certificate not retrieved from Key Vault");
                    }

                    logger.LogInformation("Server certificate retrieved from Key Vault");

                    // Get mTLS settings if available
                    var mtlsSettings = configuration.GetSection("MtlsSettings").Get<MtlsSettings>();
                    var enableMtls = mtlsSettings?.RequireClientCertificate ?? false;

                    // Configure Kestrel
                    builder.WebHost.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            if (!environment.IsDevelopment())
                            {
                                httpsOptions.ServerCertificate = serverCert;
                                
                                // Configure client certificate mode for mTLS
                                if (enableMtls)
                                {
                                    httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                                    logger.LogInformation("mTLS enabled - Client certificates REQUIRED");
                                }
                                else
                                {
                                    httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                                    logger.LogInformation("mTLS disabled - Client certificates optional");
                                }
                            }
                            else
                            {
                                // In development, make client certificates optional
                                httpsOptions.ServerCertificate = serverCert;
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                                logger.LogInformation("Development mode - Client certificates optional");
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Key Vault configuration failed");
                    throw;
                }
            }

            return services;
        }

        /// <summary>
        /// Configure mutual TLS (mTLS) client certificate authentication
        /// </summary>
        public static IServiceCollection AddMtlsAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("mTLS Client Certificate Authentication is DISABLED");
            }
            else
            {
                var mtlsSettings = configuration.GetSection("MtlsSettings").Get<MtlsSettings>()
                    ?? throw new InvalidOperationException("MtlsSettings not found");

                logger.LogInformation("Configuring mTLS certificate authentication");

                // Evaluate and log the configuration eagerly so that tests and operators can
                // see what is configured at startup time (not deferred inside AddCertificate).
                if (mtlsSettings.AllowSelfSignedCertificates && mtlsSettings.AllowCertificateChains)
                {
                    logger.LogWarning("mTLS: Allowing both chained AND self-signed certificates");
                }
                else if (mtlsSettings.AllowSelfSignedCertificates)
                {
                    logger.LogWarning("mTLS: Allowing self-signed certificates only");
                }
                else
                {
                    logger.LogInformation("mTLS: Allowing chained certificates only (recommended)");
                }

                logger.LogInformation("mTLS: Certificate revocation check = {RevocationCheck}",
                    mtlsSettings.CheckCertificateRevocation ? "ENABLED" : "DISABLED");

                services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                    .AddCertificate(options =>
                    {
                        // Configure certificate types
                        if (mtlsSettings.AllowSelfSignedCertificates && mtlsSettings.AllowCertificateChains)
                        {
                            options.AllowedCertificateTypes = CertificateTypes.All;
                        }
                        else if (mtlsSettings.AllowSelfSignedCertificates)
                        {
                            options.AllowedCertificateTypes = CertificateTypes.SelfSigned;
                        }
                        else
                        {
                            options.AllowedCertificateTypes = CertificateTypes.Chained;
                        }

                        // Configure revocation mode
                        options.RevocationMode = mtlsSettings.CheckCertificateRevocation 
                            ? X509RevocationMode.Online 
                            : X509RevocationMode.NoCheck;

                        // Configure authentication events
                        options.Events = new CertificateAuthenticationEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                logger.LogError("mTLS Authentication FAILED: {Error}", context.Exception?.Message);
                                return Task.CompletedTask;
                            },
                            OnCertificateValidated = context =>
                            {
                                logger.LogInformation("mTLS Authentication SUCCEEDED for certificate: {Subject}", 
                                    context.ClientCertificate.Subject);
                                
                                if (mtlsSettings.ValidateClientCertificateIssuer)
                                {
                                    var issuer = context.ClientCertificate.Issuer;
                                    if (!mtlsSettings.IsIssuerAllowed(issuer))
                                    {
                                        logger.LogError(
                                            "mTLS: Certificate issuer '{Issuer}' is not in the allowed list. Allowed: [{Allowed}]",
                                            issuer,
                                            string.Join(", ", mtlsSettings.AllowedIssuers ?? new List<string>()));
                                        context.Fail("Certificate issuer not trusted");
                                    }
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });

                logger.LogInformation("mTLS certificate authentication configured successfully");

                // Warn operators when issuer validation is on but no issuers are configured —
                // this causes all client certificates to be rejected (fail-closed) and is
                // likely a misconfiguration.
                if (mtlsSettings.ValidateClientCertificateIssuer &&
                    (mtlsSettings.AllowedIssuers == null || mtlsSettings.AllowedIssuers.Count == 0))
                {
                    logger.LogWarning(
                        "mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. " +
                        "All client certificates will be rejected. " +
                        "Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.");
                }
            }

            return services;
        }

        /// <summary>
        /// Configure nonce-based CSP services
        /// </summary>
        public static IServiceCollection AddNonceServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Nonce Services are DISABLED");
            }
            else
            {
                var nonceSettings = configuration.GetSection("NonceEncryption").Get<NonceEncryptionSettings>()
                    ?? throw new InvalidOperationException("NonceEncryptionSettings not found");

                services.AddSingleton<INonceEncryptionSettingsService>(new NonceEncryptionSettingsService(nonceSettings));
                services.AddSingleton<INonceRefresherService, NonceRefresherService>();
                services.AddSingleton<INonceCatalogService, NonceCatalogService>();

                // CSP Builder
                services.Configure<CSPScriptHashSettings>(configuration.GetSection("CSPScriptHashes"));
                services.AddSingleton<ContentSecurityPolicyBuilder>();

                logger.LogInformation("Nonce and CSP services configured");
            }

            return services;
        }

        /// <summary>
        /// Configure Azure Blob Storage
        /// </summary>
        public static IServiceCollection AddBlobStorageServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Blob Storage is DISABLED");
            }
            else
            {
                var blobSettings = configuration.GetSection("BlobSettings").Get<BlobSettings>()
                    ?? throw new InvalidOperationException("BlobSettings not found");

                if (string.IsNullOrEmpty(blobSettings.BlobConnectionString))
                {
                    throw new InvalidOperationException("BlobConnectionString is null or empty");
                }

                logger.LogInformation("BlobSettings configured with MaxAttachments: {Max}", blobSettings.MaxAttachments);

                services.AddSingleton(blobSettings);
                services.AddScoped<IBlobSettingsService, BlobSettingsService>();
            }

            return services;
        }

        /// <summary>
        /// Configure Cosmos DB
        /// </summary>
        public static async Task<IServiceCollection> AddCosmosDbServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("Cosmos DB is DISABLED");
            }
            else
            {
                try
                {
                    var cosmosSettings = configuration.GetSection("CosmosDb").Get<CosmosDbSettings>()
                        ?? throw new InvalidOperationException("CosmosDbSettings not found");

                    logger.LogInformation("Cosmos connection string configured: {Present}",
                        !string.IsNullOrEmpty(cosmosSettings.CosmosConnectionString));

                    // Verify connection
                    var client = new CosmosClient(cosmosSettings.CosmosConnectionString);
                    var database = client.GetDatabase(cosmosSettings.DatabaseName);
                    await database.ReadAsync();

                    logger.LogInformation("Cosmos DB connection verified: {Endpoint}", client.Endpoint);

                    services.AddSingleton<ICosmosDbSettingsService>(new CosmosDbSettingsService(cosmosSettings));
                    services.AddSingleton(client);
                    services.AddSingleton<CosmosDbService>(provider =>
                    {
                        var dbClient = provider.GetRequiredService<CosmosClient>();
                        return new CosmosDbService(dbClient, cosmosSettings.DatabaseName);
                    });

                    logger.LogInformation("Cosmos DB services configured");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cosmos DB configuration failed");
                    throw;
                }
            }

            return services;
        }

        /// <summary>
        /// Configure AWS Secrets Manager (parallel to Azure Key Vault).
        /// </summary>
        public static IServiceCollection AddAwsSecretsManagerServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("AWS Secrets Manager is DISABLED");
            }
            else
            {
                var settings = configuration.GetSection("AwsSecretsManager").Get<AwsSecretsManagerSettings>()
                    ?? throw new InvalidOperationException("AwsSecretsManagerSettings not found");

                logger.LogInformation("Configuring AWS Secrets Manager for region: {Region}", settings.Region);

                services.AddSingleton<IAwsSecretsManagerSettingsService>(new AwsSecretsManagerSettingsService(settings));
                services.AddSingleton<IAwsSecretManagerOperations, AwsSecretManagerOperations>();
                services.AddSingleton<IAwsSecretsManagerOperationsService, AwsSecretsManagerOperationsService>();

                logger.LogInformation("AWS Secrets Manager services configured");
            }

            return services;
        }

        /// <summary>
        /// Configure Amazon DynamoDB (parallel to Azure Cosmos DB).
        /// </summary>
        public static async Task<IServiceCollection> AddAwsDynamoDbServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("AWS DynamoDB is DISABLED");
            }
            else
            {
                try
                {
                    var settings = configuration.GetSection("AwsDynamoDb").Get<AwsDynamoDbSettings>()
                        ?? throw new InvalidOperationException("AwsDynamoDbSettings not found");

                    logger.LogInformation("Configuring AWS DynamoDB for region: {Region}, table: {Table}",
                        settings.Region, settings.TableName);

                    var credentials = new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey);
                    var region = RegionEndpoint.GetBySystemName(settings.Region);
                    var dynamoClient = new AmazonDynamoDBClient(credentials, region);

                    // Verify connection by describing the table
                    await dynamoClient.DescribeTableAsync(settings.TableName);

                    logger.LogInformation("AWS DynamoDB table verified: {Table}", settings.TableName);

                    services.AddSingleton<IAwsDynamoDbSettingsService>(new AwsDynamoDbSettingsService(settings));
                    services.AddSingleton<IAmazonDynamoDB>(dynamoClient);
                    services.AddSingleton<AwsDynamoDbService>(provider =>
                    {
                        var client = provider.GetRequiredService<IAmazonDynamoDB>();
                        return new AwsDynamoDbService(client, settings.TableName);
                    });

                    logger.LogInformation("AWS DynamoDB services configured");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AWS DynamoDB configuration failed");
                    throw;
                }
            }

            return services;
        }

        /// <summary>
        /// Configure Google Cloud Secret Manager (parallel to Azure Key Vault).
        /// </summary>
        public static IServiceCollection AddGcpSecretManagerServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("GCP Secret Manager is DISABLED");
            }
            else
            {
                var settings = configuration.GetSection("GcpSecretManager").Get<GcpSecretManagerSettings>()
                    ?? throw new InvalidOperationException("GcpSecretManagerSettings not found");

                logger.LogInformation("Configuring GCP Secret Manager for project: {Project}", settings.ProjectId);

                services.AddSingleton<IGcpSecretManagerSettingsService>(new GcpSecretManagerSettingsService(settings));
                services.AddSingleton<IGcpSecretManagerOperations, GcpSecretManagerOperations>();
                services.AddSingleton<IGcpSecretManagerOperationsService, GcpSecretManagerOperationsService>();

                logger.LogInformation("GCP Secret Manager services configured");
            }

            return services;
        }

        /// <summary>
        /// Configure AWS Cognito OpenID Connect authentication.
        /// Mirrors <see cref="AddAzureAdAuthentication"/> for Microsoft Entra ID / Azure AD.
        /// AWS Cognito User Pools expose a standards-compliant OIDC discovery endpoint that ASP.NET Core
        /// can consume directly via the built-in OpenID Connect middleware.
        /// </summary>
        public static IServiceCollection AddAwsCognitoAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("AWS Cognito Authentication is DISABLED");
            }
            else
            {
                var settings = configuration.GetSection("AwsCognito").Get<AwsCognitoSettings>()
                    ?? throw new InvalidOperationException("AwsCognitoSettings not found");

                // The OIDC authority for a Cognito User Pool is:
                // https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}
                var authority = $"https://cognito-idp.{settings.Region}.amazonaws.com/{settings.UserPoolId}";

                logger.LogInformation("Configuring AWS Cognito authentication for User Pool: {UserPoolId} in region: {Region}",
                    settings.UserPoolId, settings.Region);

                services.AddSingleton<IAwsCognitoSettingsService>(new AwsCognitoSettingsService(settings));

                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "AwsCognito";
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect("AwsCognito", options =>
                {
                    options.Authority = authority;
                    options.ClientId = settings.AppClientId;
                    options.ClientSecret = settings.AppClientSecret;
                    options.ResponseType = "code";
                    options.CallbackPath = settings.CallbackPath;
                    options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            var safePath = context.Request.Path.Value?.Replace("\r", "").Replace("\n", "") ?? string.Empty;
                            logger.LogInformation("AWS Cognito: Redirecting to identity provider for path: {Path}", safePath);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            logger.LogError("AWS Cognito: Authentication failed: {Error}", context.Exception?.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            logger.LogInformation("AWS Cognito: Token validated successfully");
                            return Task.CompletedTask;
                        }
                    };
                });

                services.ConfigureApplicationCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var safePath = context.Request.Path.Value?.Replace("\r", "").Replace("\n", "") ?? string.Empty;
                        logger.LogWarning("AWS Cognito: Redirect to login for path: {Path}", safePath);
                        return Task.CompletedTask;
                    };
                });

                services.AddAuthorization();

                logger.LogInformation("AWS Cognito authentication configured with authority: {Authority}", authority);
            }

            return services;
        }

        /// <summary>
        /// Configure Google Cloud Identity Platform (Google OAuth 2.0 / OpenID Connect) authentication.
        /// Mirrors <see cref="AddAzureAdAuthentication"/> for Microsoft Entra ID / Azure AD.
        /// Google's OIDC endpoint at https://accounts.google.com is consumed directly via the
        /// built-in ASP.NET Core OpenID Connect middleware.
        /// </summary>
        public static IServiceCollection AddGcpIdentityAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("GCP Identity Authentication is DISABLED");
            }
            else
            {
                var settings = configuration.GetSection("GcpIdentity").Get<GcpIdentitySettings>()
                    ?? throw new InvalidOperationException("GcpIdentitySettings not found");

                // Google's OIDC discovery endpoint
                const string authority = "https://accounts.google.com";

                var projectInfo = string.IsNullOrEmpty(settings.ProjectId)
                    ? string.Empty
                    : $" (project: {settings.ProjectId})";

                logger.LogInformation("Configuring GCP Identity authentication{ProjectInfo}", projectInfo);

                services.AddSingleton<IGcpIdentitySettingsService>(new GcpIdentitySettingsService(settings));

                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "GcpIdentity";
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect("GcpIdentity", options =>
                {
                    options.Authority = authority;
                    options.ClientId = settings.ClientId;
                    options.ClientSecret = settings.ClientSecret;
                    options.ResponseType = "code";
                    options.CallbackPath = settings.CallbackPath;
                    options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            var safePath = context.Request.Path.Value?.Replace("\r", "").Replace("\n", "") ?? string.Empty;
                            logger.LogInformation("GCP Identity: Redirecting to identity provider for path: {Path}", safePath);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            logger.LogError("GCP Identity: Authentication failed: {Error}", context.Exception?.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            logger.LogInformation("GCP Identity: Token validated successfully");
                            return Task.CompletedTask;
                        }
                    };
                });

                services.ConfigureApplicationCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var safePath = context.Request.Path.Value?.Replace("\r", "").Replace("\n", "") ?? string.Empty;
                        logger.LogWarning("GCP Identity: Redirect to login for path: {Path}", safePath);
                        return Task.CompletedTask;
                    };
                });

                services.AddAuthorization();

                logger.LogInformation("GCP Identity authentication configured with authority: {Authority}", authority);
            }

            return services;
        }

        /// <summary>
        /// Configure Google Cloud Firestore (parallel to Azure Cosmos DB).
        /// </summary>
        public static async Task<IServiceCollection> AddGcpFirestoreServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            bool enabled = true)
        {
            if (!enabled)
            {
                logger.LogWarning("GCP Firestore is DISABLED");
            }
            else
            {
                try
                {
                    var settings = configuration.GetSection("GcpFirestore").Get<GcpFirestoreSettings>()
                        ?? throw new InvalidOperationException("GcpFirestoreSettings not found");

                    logger.LogInformation("Configuring GCP Firestore for project: {Project}, collection: {Collection}",
                        settings.ProjectId, settings.CollectionName);

                    GoogleCredential? gcpCredential = null;
                    if (!string.IsNullOrEmpty(settings.CredentialFilePath))
                    {
                        await using var credStream = File.OpenRead(settings.CredentialFilePath);
                        // GoogleCredential.FromStream is the stable cross-version API for loading
                        // service-account JSON files; the obsolete warning is suppressed here
                        // because the replacement CredentialFactory API has not yet stabilised
                        // across all Google.Apis.Auth releases targeted by this project.
#pragma warning disable CS0618
                        gcpCredential = GoogleCredential.FromStream(credStream);
#pragma warning restore CS0618
                    }

                    var firestoreDb = gcpCredential is null
                        ? await FirestoreDb.CreateAsync(settings.ProjectId)
                        : await new FirestoreDbBuilder
                          {
                              ProjectId = settings.ProjectId,
                              GoogleCredential = gcpCredential
                          }.BuildAsync();

                    logger.LogInformation("GCP Firestore connected to project: {Project}", settings.ProjectId);

                    services.AddSingleton<IGcpFirestoreSettingsService>(new GcpFirestoreSettingsService(settings));
                    services.AddSingleton(firestoreDb);
                    services.AddSingleton<GcpFirestoreService>(provider =>
                    {
                        var db = provider.GetRequiredService<FirestoreDb>();
                        return new GcpFirestoreService(db, settings.CollectionName);
                    });

                    logger.LogInformation("GCP Firestore services configured");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GCP Firestore configuration failed");
                    throw;
                }
            }

            return services;
        }
    }
}