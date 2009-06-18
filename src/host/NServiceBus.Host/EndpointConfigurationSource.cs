using System;
using System.Configuration;
using NServiceBus.Config.ConfigurationSource;

namespace NServiceBus.Host
{
    public class EndpointConfigurationSource:IConfigurationSource
    {
        private readonly Type messageEndpointType;

        public EndpointConfigurationSource(Type messageEndpointType)
        {
            this.messageEndpointType = messageEndpointType;
        }

        public T GetConfiguration<T>() where T : class
        {
            return ConfigurationManager
                    .OpenExeConfiguration(messageEndpointType.Assembly.ManifestModule.Name)
                    .GetSection(typeof(T).Name) as T;
        }
    }
}