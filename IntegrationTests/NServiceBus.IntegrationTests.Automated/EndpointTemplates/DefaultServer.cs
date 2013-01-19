namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System;
    using System.Collections.Generic;

    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public void Setup(Configure config, IDictionary<string, string> settings)
        {
            config.DefaultBuilder()
                    .XmlSerializer()
                    .DefineTransport(settings["Transport"])
                    .UnicastBus();
        }
    }

    public static class ConfigureTransportExtensions
    {
        public static Configure DefineTransport(this Configure config, string transport)
        {
            if (string.IsNullOrEmpty(transport))
                return config;

            var transportType = Type.GetType(transport);

            if (DefaultConnectionStrings.ContainsKey(transportType))
                return config.UseTransport(transportType, DefaultConnectionStrings[transportType]);
            else
                return config.UseTransport(transportType);
        }

        static Dictionary<Type, string> DefaultConnectionStrings = new Dictionary<Type, string>
            {
                { typeof(RabbitMQ), "host=localhost" },
                { typeof(SqlServer), @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;" },
                { typeof(ActiveMQ),  @"activemq:tcp://localhost:61616" },
               
            };
    }
}