using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class GcpIdentitySettingsServiceTests
    {
        private static GcpIdentitySettings MakeSettings() => new GcpIdentitySettings
        {
            ClientId = "123456789-abcdefghij.apps.googleusercontent.com",
            ClientSecret = "gcp-secret-value",
            ProjectId = "my-project-123456"
        };

        [Fact]
        public void Constructor_ShouldAcceptSettings()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.Should().NotBeNull();
        }

        [Fact]
        public void GetSettings_ShouldReturnConfiguredSettings()
        {
            var settings = MakeSettings();
            var svc = new GcpIdentitySettingsService(settings);

            var result = svc.GetSettings();

            result.Should().BeSameAs(settings);
        }

        [Fact]
        public void GetSettings_ClientId_ShouldMatchInput()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.GetSettings().ClientId.Should().Be("123456789-abcdefghij.apps.googleusercontent.com");
        }

        [Fact]
        public void GetSettings_ClientSecret_ShouldMatchInput()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.GetSettings().ClientSecret.Should().Be("gcp-secret-value");
        }

        [Fact]
        public void GetSettings_ProjectId_ShouldMatchInput()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.GetSettings().ProjectId.Should().Be("my-project-123456");
        }

        [Fact]
        public void GetSettings_DefaultCallbackPath_ShouldBeSigninGcp()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.GetSettings().CallbackPath.Should().Be("/signin-gcp");
        }

        [Fact]
        public void GetSettings_DefaultSignedOutCallbackPath_ShouldBeSignoutGcp()
        {
            var svc = new GcpIdentitySettingsService(MakeSettings());
            svc.GetSettings().SignedOutCallbackPath.Should().Be("/signout-gcp");
        }

        [Fact]
        public void GetSettings_CustomCallbackPath_ShouldOverrideDefault()
        {
            var settings = MakeSettings();
            settings.CallbackPath = "/my-gcp-callback";
            var svc = new GcpIdentitySettingsService(settings);
            svc.GetSettings().CallbackPath.Should().Be("/my-gcp-callback");
        }

        [Fact]
        public void GetSettings_EmptyProjectId_ShouldBeAllowed()
        {
            var settings = new GcpIdentitySettings
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            var svc = new GcpIdentitySettingsService(settings);
            svc.GetSettings().ProjectId.Should().BeEmpty();
        }

        [Theory]
        [InlineData("client-1.apps.googleusercontent.com", "secret-1", "project-1")]
        [InlineData("client-2.apps.googleusercontent.com", "secret-2", "project-2")]
        [InlineData("client-3.apps.googleusercontent.com", "secret-3", "")]
        public void GetSettings_DifferentClients_ReturnsCorrectValues(
            string clientId, string clientSecret, string projectId)
        {
            var settings = new GcpIdentitySettings
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                ProjectId = projectId
            };
            var svc = new GcpIdentitySettingsService(settings);
            var result = svc.GetSettings();

            result.ClientId.Should().Be(clientId);
            result.ClientSecret.Should().Be(clientSecret);
            result.ProjectId.Should().Be(projectId);
        }
    }
}
