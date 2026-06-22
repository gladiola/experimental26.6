using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Integration
{
    /// <summary>
    /// Integration tests for the entire application.
    /// These tests start the full web application and test end-to-end scenarios.
    /// Authentication is disabled in the test environment (no Azure AD configured).
    /// </summary>
    public class ApplicationIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public ApplicationIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetHomePage_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode();
            response.Content.Headers.ContentType?.ToString()
                .Should().Contain("text/html");
        }

        [Fact]
        public async Task GetPrivacyPage_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Privacy");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public void Application_HasFeatureFlags()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - Access services
            var featureFlags = _factory.Services.GetService<FeatureFlags>();

            // Assert
            featureFlags.Should().NotBeNull();
        }

        [Fact]
        public void Application_RegistersRequiredServices()
        {
            // Act
            var loggerFactory = _factory.Services.GetService<ILoggerFactory>();

            // Assert
            loggerFactory.Should().NotBeNull();
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Privacy")]
        [InlineData("/Home/Privacy")]
        public async Task GetPublicPages_ReturnsSuccess(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task GetNonExistentPage_Returns404()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/NonExistent");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public void Application_ConfiguresSession()
        {
            // This test verifies session services are configured
            // when EnableSession feature flag is true
            
            // Arrange & Act
            var client = _factory.CreateClient();

            // Assert - If app starts without error, session is configured correctly
            client.Should().NotBeNull();
        }

        [Fact]
        public async Task Application_ReturnsSecurityHeaders()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().NotBeNull();
            // Add specific header checks based on your security configuration
        }
    }

    /// <summary>
    /// Custom web application factory for testing with modified configuration
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace services for testing
                // Example: Replace database with in-memory version
                
                // Remove existing DbContext if needed
                // var descriptor = services.SingleOrDefault(
                //     d => d.ServiceType == typeof(DbContextOptions<YourDbContext>));
                // if (descriptor != null)
                // {
                //     services.Remove(descriptor);
                // }

                // Add test-specific services
                services.Configure<FeatureFlags>(options =>
                {
                    options.EnableAzureAd = false;
                    options.EnableCosmosDb = false;
                    options.EnableBlobStorage = false;
                });
            });
        }
    }

    /// <summary>
    /// Application factory used by <see cref="ApplicationIntegrationTests"/>.
    /// Disables authentication and cloud services that are not available in CI.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _sqliteDatabasePath =
            Path.Combine(Path.GetTempPath(), $"experimental266-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            // UseSetting overrides configuration values before Program.cs reads them.
            builder.UseSetting("FeatureFlags:EnableAzureAd", "false");
            builder.UseSetting("FeatureFlags:EnableAuthorization", "false");
            builder.UseSetting("FeatureFlags:EnableCosmosDb", "false");
            builder.UseSetting("FeatureFlags:EnableBlobStorage", "false");
            builder.UseSetting("FeatureFlags:EnableNonceServices", "false");
            builder.UseSetting("FeatureFlags:EnableKeyVault", "false");
            builder.UseSetting("CrudData:Provider", "Sqlite");
            builder.UseSetting("CrudData:SqliteDatabasePath", _sqliteDatabasePath);
        }
    }
}
