using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public interface IGcpFirestoreSettingsService
    {
        GcpFirestoreSettings GetSettings();
    }

    public class GcpFirestoreSettingsService : IGcpFirestoreSettingsService
    {
        private readonly GcpFirestoreSettings _settings;

        public GcpFirestoreSettingsService(GcpFirestoreSettings settings)
        {
            _settings = settings;
        }

        public GcpFirestoreSettings GetSettings()
        {
            return _settings;
        }
    }
}
