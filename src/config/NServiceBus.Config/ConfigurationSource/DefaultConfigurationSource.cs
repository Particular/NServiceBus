using System;
using System.Configuration;

namespace NServiceBus.Config.ConfigurationSource
{
    public class DefaultConfigurationSource:IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class
        {
            if (!typeof(ConfigurationSection).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("DefaultConfigurationSource only supports .Net ConfigurationSections");

            return ConfigurationManager.GetSection(typeof(T).Name) as T;
        }
    }
}