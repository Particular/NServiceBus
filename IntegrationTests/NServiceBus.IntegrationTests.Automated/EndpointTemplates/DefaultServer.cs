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
                    .PurgeOnStartup(true)//not default but we need this to make sure that no leftover messages are left from a previous run. We can improve on this later
                    .UnicastBus();
        }
    }
}