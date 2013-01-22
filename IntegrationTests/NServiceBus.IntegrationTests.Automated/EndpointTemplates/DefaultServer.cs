namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using Config.ConfigurationSource;
    using NServiceBus;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior,IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            
            return Configure.With()
                    .DefineEndpointName(endpointBehavior.EndpointName)
                    .DefineBuilder(settings.GetOrNull("Builder"))
                    .CustomConfigurationSource(configSource)
                    .DefineSerializer(settings.GetOrNull("Serializer"))
                    .DefineTransport(settings.GetOrNull("Transport"))
                    .PurgeOnStartup(true)//not default but we need this to make sure that no leftover messages are left from a previous run. We can improve on this later
                    .UnicastBus();

        }
    }
}