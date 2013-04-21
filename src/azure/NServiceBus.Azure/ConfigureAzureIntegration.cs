using NServiceBus.Integration.Azure;

namespace NServiceBus
{
    public static class ConfigureAzureIntegration
    {
        public static Configure AzureConfigurationSource(this Configure config)
        {
            return AzureConfigurationSource(config, string.Empty);
        }

        public static Configure AzureConfigurationSource(this Configure config, string configurationPrefix)
        {
            var azureConfigSource = new AzureConfigurationSource(new AzureConfigurationSettings());
            azureConfigSource.ConfigurationPrefix = configurationPrefix;
            return config.CustomConfigurationSource(azureConfigSource);
        }
    }
}