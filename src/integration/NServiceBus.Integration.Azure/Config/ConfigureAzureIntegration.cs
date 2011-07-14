using NServiceBus.Integration.Azure;

namespace NServiceBus.Config
{
    public static class ConfigureAzureIntegration
    {
        public static Configure AzureConfigurationSource(this Configure config)
        {
            var azureConfigSource = new AzureConfigurationSource(new AzureConfigurationSettings());
            
            return config.CustomConfigurationSource(azureConfigSource);
        }
    }
}