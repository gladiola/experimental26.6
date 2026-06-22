using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Provides access to the Google Cloud Identity Platform settings.
    /// Mirrors the role of <see cref="IAzureADSettingsService"/> for Microsoft Entra ID / Azure AD.
    /// </summary>
    public interface IGcpIdentitySettingsService
    {
        GcpIdentitySettings GetSettings();
    }

    /// <summary>
    /// Default implementation of <see cref="IGcpIdentitySettingsService"/>.
    /// </summary>
    public class GcpIdentitySettingsService : IGcpIdentitySettingsService
    {
        private readonly GcpIdentitySettings _settings;

        public GcpIdentitySettingsService(GcpIdentitySettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public GcpIdentitySettings GetSettings()
        {
            return _settings;
        }
    }
}
