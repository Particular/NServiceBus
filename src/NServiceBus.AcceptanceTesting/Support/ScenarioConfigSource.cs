namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

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
            var type = typeof (T);

            if (configuration.UserDefinedConfigSections.ContainsKey(type))
                return configuration.UserDefinedConfigSections[type] as T;


            if (type == typeof (MessageForwardingInCaseOfFaultConfig))
                return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = "error"
                    } as T;
            
            if (type == typeof (UnicastBusConfig))
            {
                var auditAddress = configuration.AddressOfAuditQueue != null
                                       ? configuration.AddressOfAuditQueue.ToString()
                                       : null;

                if (configuration.AuditEndpoint != null)
                {
                    auditAddress = routingTable[configuration.AuditEndpoint];
                }
                
                return new UnicastBusConfig
                {
                    ForwardReceivedMessagesTo = auditAddress,
                    MessageEndpointMappings = GenerateMappings()
                } as T;

            }
                


            if (type == typeof(Logging))
                return new Logging()
                {
                    Threshold = "WARN"
                } as T;


            return ConfigurationManager.GetSection(type.Name) as T;
        }

        MessageEndpointMappingCollection GenerateMappings()
        {
            var mappings = new MessageEndpointMappingCollection();

            foreach (var templateMapping in configuration.EndpointMappings)
            {
                var messageType = templateMapping.Key;
                var endpoint = templateMapping.Value;

               mappings.Add( new MessageEndpointMapping
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