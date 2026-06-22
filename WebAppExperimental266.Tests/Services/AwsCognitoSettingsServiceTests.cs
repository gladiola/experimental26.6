using WebAppExperimental266.Models.Settings;
using WebAppExperimental266.Services;

namespace WebAppExperimental266.Tests.Services
{
    public class AwsCognitoSettingsServiceTests
    {
        private static AwsCognitoSettings MakeSettings() => new AwsCognitoSettings
        {
            Region = "us-east-1",
            UserPoolId = "us-east-1_AbCdEfGhI",
            AppClientId = "1abc2def3ghi4jkl5mno6pqr7",
            AppClientSecret = "secret123",
            Domain = "my-app.auth.us-east-1.amazoncognito.com"
        };

        [Fact]
        public void Constructor_ShouldAcceptSettings()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.Should().NotBeNull();
        }

        [Fact]
        public void GetSettings_ShouldReturnConfiguredSettings()
        {
            var settings = MakeSettings();
            var svc = new AwsCognitoSettingsService(settings);

            var result = svc.GetSettings();

            result.Should().BeSameAs(settings);
        }

        [Fact]
        public void GetSettings_Region_ShouldMatchInput()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().Region.Should().Be("us-east-1");
        }

        [Fact]
        public void GetSettings_UserPoolId_ShouldMatchInput()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().UserPoolId.Should().Be("us-east-1_AbCdEfGhI");
        }

        [Fact]
        public void GetSettings_AppClientId_ShouldMatchInput()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().AppClientId.Should().Be("1abc2def3ghi4jkl5mno6pqr7");
        }

        [Fact]
        public void GetSettings_Domain_ShouldMatchInput()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().Domain.Should().Be("my-app.auth.us-east-1.amazoncognito.com");
        }

        [Fact]
        public void GetSettings_DefaultCallbackPath_ShouldBeSigninAwsCognito()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().CallbackPath.Should().Be("/signin-aws-cognito");
        }

        [Fact]
        public void GetSettings_DefaultSignedOutCallbackPath_ShouldBeSignoutAwsCognito()
        {
            var svc = new AwsCognitoSettingsService(MakeSettings());
            svc.GetSettings().SignedOutCallbackPath.Should().Be("/signout-aws-cognito");
        }

        [Fact]
        public void GetSettings_CustomCallbackPath_ShouldOverrideDefault()
        {
            var settings = MakeSettings();
            settings.CallbackPath = "/custom-callback";
            var svc = new AwsCognitoSettingsService(settings);
            svc.GetSettings().CallbackPath.Should().Be("/custom-callback");
        }

        [Theory]
        [InlineData("us-east-1", "us-east-1_Pool1", "client1", "secret1", "app.auth.us-east-1.amazoncognito.com")]
        [InlineData("eu-west-1", "eu-west-1_Pool2", "client2", "secret2", "app.auth.eu-west-1.amazoncognito.com")]
        [InlineData("ap-southeast-1", "ap-southeast-1_Pool3", "client3", "secret3", "app.auth.ap-southeast-1.amazoncognito.com")]
        public void GetSettings_DifferentRegions_ReturnsCorrectValues(
            string region, string userPoolId, string clientId, string clientSecret, string domain)
        {
            var settings = new AwsCognitoSettings
            {
                Region = region,
                UserPoolId = userPoolId,
                AppClientId = clientId,
                AppClientSecret = clientSecret,
                Domain = domain
            };
            var svc = new AwsCognitoSettingsService(settings);
            var result = svc.GetSettings();

            result.Region.Should().Be(region);
            result.UserPoolId.Should().Be(userPoolId);
            result.AppClientId.Should().Be(clientId);
            result.Domain.Should().Be(domain);
        }
    }
}
