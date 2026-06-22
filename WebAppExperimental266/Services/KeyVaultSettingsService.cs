using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{

    public interface IKeyVaultSettingsService { 
        
        KeyVaultSettings GetSettings();

    }
    public class KeyVaultSettingsService : IKeyVaultSettingsService
    {
        private readonly KeyVaultSettings _settings;

        public KeyVaultSettingsService(KeyVaultSettings kvs) { 
        
            _settings = kvs;
        
        }

        public KeyVaultSettings GetSettings()
        {
            return _settings;
        }
    }
}
