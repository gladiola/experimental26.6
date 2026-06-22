using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public interface IAwsDynamoDbSettingsService
    {
        AwsDynamoDbSettings GetSettings();
    }

    public class AwsDynamoDbSettingsService : IAwsDynamoDbSettingsService
    {
        private readonly AwsDynamoDbSettings _settings;

        public AwsDynamoDbSettingsService(AwsDynamoDbSettings settings)
        {
            _settings = settings;
        }

        public AwsDynamoDbSettings GetSettings()
        {
            return _settings;
        }
    }
}
