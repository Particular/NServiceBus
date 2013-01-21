namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using NServiceBus;
    using System.Collections.Generic;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public void Setup(Configure config, IDictionary<string, string> settings)
        {
            config.DefineBuilder(settings.GetOrNull("Builder"))
                    .DefineSerializer(settings.GetOrNull("Serializer"))
                    .DefineTransport(settings.GetOrNull("Transport"))
                    .UnicastBus();
        }
    }
}