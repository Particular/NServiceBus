using System;
using System.Configuration;

namespace NServiceBus.Config.ConfigurationSource
{
    /// <summary>
    /// A configuration source implementation on top of ConfigurationManager.
    /// </summary>
    public class DefaultConfigurationSource : IConfigurationSource
    {
        T IConfigurationSource.GetConfiguration<T>()
        {
            if (!typeof(ConfigurationSection).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("DefaultConfigurationSource only supports .Net ConfigurationSections");

            return ConfigurationManager.GetSection(typeof(T).Name) as T;
        }
    }
}