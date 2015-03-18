namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using Config;
    using Config.ConfigurationSource;

    public class ScenarioConfigSource : IConfigurationSource
    {
        readonly EndpointConfiguration configuration;
        readonly IDictionary<Type, string> routingTable;

        public ScenarioConfigSource(EndpointConfiguration configuration, IDictionary<Type, string> routingTable)
        {
            this.configuration = configuration;
            this.routingTable = routingTable;
        }

        public T GetConfiguration<T>() where T : class, new()
        {
            var type = typeof(T);

            object configurationSection;
            if (configuration.UserDefinedConfigSections.TryGetValue(type, out configurationSection))
            {
                return configurationSection as T;
            }


            if (type == typeof(MessageForwardingInCaseOfFaultConfig))
            {
                if (!configuration.SendOnly)
                {
                    return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = "error"
                    } as T;
                }
            }

            if (type == typeof(UnicastBusConfig))
            {

                return new UnicastBusConfig
                {
                    MessageEndpointMappings = GenerateMappings()
                } as T;

            }

            if (type == typeof(AuditConfig))
            {
                if (!configuration.SendOnly)
                {
                    if (configuration.AddressOfAuditQueue != null)
                    {
                        return new AuditConfig { QueueName = configuration.AddressOfAuditQueue } as T;
                    }

                    if (configuration.AuditEndpoint != null)
                    {
                        return new AuditConfig { QueueName = routingTable[configuration.AuditEndpoint] } as T;
                    }
                }
            }

            if (type == typeof(Logging))
            {
                return new Logging
                {
                    Threshold = "WARN"
                } as T;
            }


            return ConfigurationManager.GetSection(type.Name) as T;
        }

        MessageEndpointMappingCollection GenerateMappings()
        {
            var mappings = new MessageEndpointMappingCollection();

            foreach (var templateMapping in configuration.EndpointMappings)
            {
                var messageType = templateMapping.Key;
                var endpoint = templateMapping.Value;

                mappings.Add(new MessageEndpointMapping
                     {
                         AssemblyName = messageType.Assembly.FullName,
                         TypeFullName = messageType.FullName,
                         Endpoint = routingTable[endpoint]
                     });
            }

            return mappings;
        }
    }
}