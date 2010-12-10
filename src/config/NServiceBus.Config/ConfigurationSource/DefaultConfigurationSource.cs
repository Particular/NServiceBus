using System;
using System.Configuration;
using System.IO;

namespace NServiceBus.Config.ConfigurationSource
{
    /// <summary>
    /// A configuration source implementation on top of ConfigurationManager.
    /// </summary>
    public class DefaultConfigurationSource : IConfigurationSource
    {
        static bool configurationIsValid;

        T IConfigurationSource.GetConfiguration<T>()
        {
            ValidateConfiguration();
            if (!typeof(ConfigurationSection).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("DefaultConfigurationSource only supports .Net ConfigurationSections");

            return ConfigurationManager.GetSection(typeof(T).Name) as T;
        }

        void ValidateConfiguration()
        {
            if (configurationIsValid)
                return;

            var endpointConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            if (!File.Exists(endpointConfigurationFile))
            {
                throw new InvalidOperationException(string.Format("No configuration file found at: {0}, the default configuration for nservicebus requires an app.config to be present. Please add one!",endpointConfigurationFile));
            }

            configurationIsValid = true;
        }
    }
}