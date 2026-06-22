using FluentAssertions;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Tests.Models
{
    /// <summary>
    /// Unit tests for OcspSettings configuration model
    /// </summary>
    public class OcspSettingsTests
    {
        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            // Act
            var settings = new OcspSettings();

            // Assert
            settings.EnableOcspValidation.Should().BeFalse("OCSP validation should be disabled by default");
            settings.RequestTimeoutSeconds.Should().Be(30, "default timeout should be 30 seconds");
            settings.MaxRetryAttempts.Should().Be(3, "default retry attempts should be 3");
            settings.CacheDurationMinutes.Should().Be(60, "default cache duration should be 60 minutes");
            settings.ServerUnavailableBehavior.Should().Be("Warn", "default behavior should be Warn");
            settings.EnableDetailedLogging.Should().BeFalse("detailed logging should be disabled by default");
            settings.SkipValidationInDevelopment.Should().BeTrue("skip validation in dev by default");
            settings.OcspServerUrl.Should().BeNull("OCSP server URL should be null by default");
        }

        [Fact]
        public void AllProperties_CanBeSet()
        {
            // Act
            var settings = new OcspSettings
            {
                EnableOcspValidation = true,
                OcspServerUrl = "https://ocsp.example.com",
                RequestTimeoutSeconds = 60,
                MaxRetryAttempts = 5,
                CacheDurationMinutes = 120,
                ServerUnavailableBehavior = "Fail",
                EnableDetailedLogging = true,
                SkipValidationInDevelopment = false
            };

            // Assert
            settings.EnableOcspValidation.Should().BeTrue();
            settings.OcspServerUrl.Should().Be("https://ocsp.example.com");
            settings.RequestTimeoutSeconds.Should().Be(60);
            settings.MaxRetryAttempts.Should().Be(5);
            settings.CacheDurationMinutes.Should().Be(120);
            settings.ServerUnavailableBehavior.Should().Be("Fail");
            settings.EnableDetailedLogging.Should().BeTrue();
            settings.SkipValidationInDevelopment.Should().BeFalse();
        }

        [Theory]
        [InlineData("Fail")]
        [InlineData("Allow")]
        [InlineData("Warn")]
        public void ServerUnavailableBehavior_AcceptsValidValues(string behavior)
        {
            // Act
            var settings = new OcspSettings
            {
                ServerUnavailableBehavior = behavior
            };

            // Assert
            settings.ServerUnavailableBehavior.Should().Be(behavior);
        }

        [Theory]
        [InlineData("http://ocsp.example.com")]
        [InlineData("https://ocsp.example.com")]
        [InlineData("https://ocsp.example.com:8080/ocsp")]
        [InlineData("http://192.168.1.100/ocsp")]
        public void OcspServerUrl_AcceptsValidUrls(string url)
        {
            // Act
            var settings = new OcspSettings
            {
                OcspServerUrl = url
            };

            // Assert
            settings.OcspServerUrl.Should().Be(url);
        }

        [Fact]
        public void OcspServerUrl_CanBeNull()
        {
            // Act
            var settings = new OcspSettings
            {
                OcspServerUrl = null
            };

            // Assert
            settings.OcspServerUrl.Should().BeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(300)]
        public void RequestTimeoutSeconds_AcceptsPositiveValues(int timeout)
        {
            // Act
            var settings = new OcspSettings
            {
                RequestTimeoutSeconds = timeout
            };

            // Assert
            settings.RequestTimeoutSeconds.Should().Be(timeout);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void MaxRetryAttempts_AcceptsValidValues(int retries)
        {
            // Act
            var settings = new OcspSettings
            {
                MaxRetryAttempts = retries
            };

            // Assert
            settings.MaxRetryAttempts.Should().Be(retries);
        }

        [Theory]
        [InlineData(0)]  // No cache
        [InlineData(60)] // 1 hour
        [InlineData(1440)] // 1 day
        public void CacheDurationMinutes_AcceptsValidValues(int duration)
        {
            // Act
            var settings = new OcspSettings
            {
                CacheDurationMinutes = duration
            };

            // Assert
            settings.CacheDurationMinutes.Should().Be(duration);
        }

        [Fact]
        public void ProductionConfiguration_HasSecureDefaults()
        {
            // Arrange - Production scenario
            var settings = new OcspSettings
            {
                EnableOcspValidation = true,
                OcspServerUrl = "https://ocsp.production.com",
                RequestTimeoutSeconds = 30,
                MaxRetryAttempts = 3,
                ServerUnavailableBehavior = "Fail",
                EnableDetailedLogging = false,
                SkipValidationInDevelopment = false
            };

            // Assert
            settings.EnableOcspValidation.Should().BeTrue("production should enable OCSP");
            settings.ServerUnavailableBehavior.Should().Be("Fail", "production should fail on unavailable server");
            settings.SkipValidationInDevelopment.Should().BeFalse("production should not skip validation");
        }

        [Fact]
        public void DevelopmentConfiguration_AllowsFlexibility()
        {
            // Arrange - Development scenario
            var settings = new OcspSettings
            {
                EnableOcspValidation = false,
                SkipValidationInDevelopment = true,
                ServerUnavailableBehavior = "Allow",
                EnableDetailedLogging = true
            };

            // Assert
            settings.EnableOcspValidation.Should().BeFalse("development may disable OCSP");
            settings.SkipValidationInDevelopment.Should().BeTrue("development should skip validation");
            settings.ServerUnavailableBehavior.Should().Be("Allow", "development should allow on errors");
            settings.EnableDetailedLogging.Should().BeTrue("development may enable detailed logging");
        }

        [Fact]
        public void SettingsObject_IsSerializable()
        {
            // Arrange
            var settings = new OcspSettings
            {
                EnableOcspValidation = true,
                OcspServerUrl = "https://ocsp.test.com",
                RequestTimeoutSeconds = 45
            };

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(settings);

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("EnableOcspValidation");
            json.Should().Contain("OcspServerUrl");
            json.Should().Contain("ocsp.test.com");
        }

        [Fact]
        public void SettingsObject_IsDeserializable()
        {
            // Arrange
            var json = @"{
                ""EnableOcspValidation"": true,
                ""OcspServerUrl"": ""https://ocsp.example.com"",
                ""RequestTimeoutSeconds"": 60,
                ""MaxRetryAttempts"": 5,
                ""CacheDurationMinutes"": 120,
                ""ServerUnavailableBehavior"": ""Fail"",
                ""EnableDetailedLogging"": true,
                ""SkipValidationInDevelopment"": false
            }";

            // Act
            var settings = System.Text.Json.JsonSerializer.Deserialize<OcspSettings>(json);

            // Assert
            settings.Should().NotBeNull();
            settings!.EnableOcspValidation.Should().BeTrue();
            settings.OcspServerUrl.Should().Be("https://ocsp.example.com");
            settings.RequestTimeoutSeconds.Should().Be(60);
            settings.MaxRetryAttempts.Should().Be(5);
        }

        [Fact]
        public void MultipleInstances_AreIndependent()
        {
            // Arrange
            var settings1 = new OcspSettings { EnableOcspValidation = true };
            var settings2 = new OcspSettings { EnableOcspValidation = false };

            // Assert
            settings1.EnableOcspValidation.Should().BeTrue();
            settings2.EnableOcspValidation.Should().BeFalse();
            settings1.Should().NotBeSameAs(settings2);
        }
    }
}
