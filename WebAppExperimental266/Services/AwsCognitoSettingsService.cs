using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Provides access to the AWS Cognito identity settings.
    /// Mirrors the role of <see cref="IAzureADSettingsService"/> for Microsoft Entra ID / Azure AD.
    /// </summary>
    public interface IAwsCognitoSettingsService
    {
        AwsCognitoSettings GetSettings();
    }

    /// <summary>
    /// Default implementation of <see cref="IAwsCognitoSettingsService"/>.
    /// </summary>
    public class AwsCognitoSettingsService : IAwsCognitoSettingsService
    {
        private readonly AwsCognitoSettings _settings;

        public AwsCognitoSettingsService(AwsCognitoSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public AwsCognitoSettings GetSettings()
        {
            return _settings;
        }
    }
}
