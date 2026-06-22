using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebAppExperimental266.Data;
using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Extensions
{
    public static class CrudAndAdminExtensions
    {
        public static IServiceCollection AddFallbackAuthentication(
            this IServiceCollection services,
            ILogger logger)
        {
            services.AddAuthentication("DisabledAuthentication")
                .AddScheme<AuthenticationSchemeOptions, DisabledAuthenticationHandler>(
                    "DisabledAuthentication",
                    _ => { });

            logger.LogWarning(
                "No interactive authentication provider was enabled. Falling back to a disabled authentication scheme that returns 401/403 for protected routes.");

            return services;
        }

        public static IServiceCollection AddCrudDataServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger,
            IWebHostEnvironment environment)
        {
            var crudSettings = configuration.GetSection("CrudData").Get<CrudDataSettings>()
                ?? new CrudDataSettings();

            services.AddSingleton(crudSettings);
            services.AddDbContext<CrudDbContext>((serviceProvider, options) =>
            {
                var settings = serviceProvider.GetRequiredService<CrudDataSettings>();
                if (settings.UseCosmos)
                {
                    var cosmosSettings = configuration.GetSection("CosmosDb").Get<CosmosDbSettings>()
                        ?? throw new InvalidOperationException(
                            "CosmosDb settings are required when CrudData:Provider is set to Cosmos.");

                    var connectionString = !string.IsNullOrWhiteSpace(cosmosSettings.CosmosConnectionString)
                        ? cosmosSettings.CosmosConnectionString
                        : $"AccountEndpoint={cosmosSettings.AccountEndpoint};AccountKey={cosmosSettings.AccountKey};";

                    options.UseCosmos(connectionString, cosmosSettings.DatabaseName);
                }
                else
                {
                    var databasePath = settings.ResolveSqliteDatabasePath(environment.ContentRootPath);
                    var directory = Path.GetDirectoryName(databasePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    options.UseSqlite($"Data Source={databasePath}");
                }
            });

            logger.LogInformation(
                "CRUD data services configured for provider {Provider}",
                crudSettings.Provider);

            return services;
        }

        public static IServiceCollection AddAdminCertificateAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            var settings = configuration.GetSection("AdminCertificateSettings").Get<AdminCertificateSettings>()
                ?? new AdminCertificateSettings();

            services.AddSingleton(settings);
            services.AddHttpContextAccessor();
            services.AddSingleton<IAdminCertificateAuditService, AdminCertificateAuditService>();
            services.AddSingleton<IAuthorizationHandler, AdminCertificateRequirementHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminCertificate", policy =>
                    policy.RequireAuthenticatedUser()
                          .AddRequirements(new AdminCertificateRequirement()));
            });

            if (settings.AllowedIssuers.Count == 0)
            {
                logger.LogWarning(
                    "Admin certificate authorization is enabled but AdminCertificateSettings:AllowedIssuers is empty. " +
                    "All admin page requests will be rejected until a dedicated admin issuer is configured.");
            }

            if (settings.AdminUsers.Count == 0)
            {
                logger.LogWarning(
                    "Admin certificate authorization is enabled but no AdminCertificateSettings:AdminUsers mappings were configured.");
            }

            return services;
        }

        public static async Task<IApplicationBuilder> EnsureCrudDataStoreCreatedAsync(
            this IApplicationBuilder app,
            ILogger logger)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CrudDbContext>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var settings = scope.ServiceProvider.GetRequiredService<CrudDataSettings>();

            if (environment.IsDevelopment() || string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            else
            {
                logger.LogInformation(
                    "Skipping automatic CRUD data-store creation for provider {Provider} outside Development/Testing. Provision the schema or container separately.",
                    settings.Provider);
                return app;
            }

            logger.LogInformation(
                "CRUD data store is ready using provider {Provider}",
                dbContext.Database.ProviderName ?? "(unknown)");

            return app;
        }
    }
}
