namespace NServiceBus.IntegrationTests.Automated.Support
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    public class ScenarioConfigSource : IConfigurationSource
    {
        readonly EndpointBehaviour behaviour;

        public ScenarioConfigSource(EndpointBehaviour behaviour)
        {
            this.behaviour = behaviour;
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
                        MessageEndpointMappings = behaviour.EndpointMappings
                    }as T;


            return null;
        }
    }
}