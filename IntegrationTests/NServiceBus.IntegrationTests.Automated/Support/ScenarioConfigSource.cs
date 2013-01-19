namespace NServiceBus.IntegrationTests.Automated.Support
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    public class ScenarioConfigSource : IConfigurationSource
    {
        readonly EndpointBehavior behavior;

        public ScenarioConfigSource(EndpointBehavior behavior)
        {
            this.behavior = behavior;
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
                        MessageEndpointMappings = this.behavior.EndpointMappings
                    }as T;


            return null;
        }
    }
}