using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{

    public interface ICosmosDbSettingsService {

        CosmosDbSettings GetSettings();

    }
    public class CosmosDbSettingsService : ICosmosDbSettingsService
    {
        private readonly CosmosDbSettings _settings;

        public CosmosDbSettingsService(CosmosDbSettings cdbs) { 
        
            _settings = cdbs;
        
        }

        public CosmosDbSettings GetSettings()
        {
            return _settings;
        }
    }
}
