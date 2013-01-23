namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using Config;
    using Config.ConfigurationSource;

    public class ScenarioConfigSource : IConfigurationSource
    {
        readonly EndpointBehavior behavior;
        readonly IDictionary<Type, string> routingTable;

        public ScenarioConfigSource(EndpointBehavior behavior, IDictionary<Type, string> routingTable)
        {
            this.behavior = behavior;
            this.routingTable = routingTable;
        }

        public T GetConfiguration<T>() where T : class, new()
        {
            var type = typeof (T);

            if (type == typeof (MessageForwardingInCaseOfFaultConfig))
                return new MessageForwardingInCaseOfFaultConfig
                    {
                        ErrorQueue = "error"
                    } as T;
            
            if (type == typeof(UnicastBusConfig))
                return new UnicastBusConfig
                    {
                        MessageEndpointMappings = GenerateMappings()
                    }as T;



            if (type == typeof(Logging))
                return new Logging()
                {
                    Threshold = "WARN"
                } as T;


            return null;
        }

        MessageEndpointMappingCollection GenerateMappings()
        {
            var mappings = new MessageEndpointMappingCollection();

            foreach (var templateMapping in behavior.EndpointMappings)
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