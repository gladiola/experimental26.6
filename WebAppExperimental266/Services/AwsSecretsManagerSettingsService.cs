using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    public interface IAwsSecretsManagerSettingsService
    {
        AwsSecretsManagerSettings GetSettings();
    }

    public class AwsSecretsManagerSettingsService : IAwsSecretsManagerSettingsService
    {
        private readonly AwsSecretsManagerSettings _settings;

        public AwsSecretsManagerSettingsService(AwsSecretsManagerSettings settings)
        {
            _settings = settings;
        }

        public AwsSecretsManagerSettings GetSettings()
        {
            return _settings;
        }
    }
}
