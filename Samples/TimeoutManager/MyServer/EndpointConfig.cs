namespace MyServer
{
    using NServiceBus;
    using NServiceBus.MessageMutator;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                //shows multi tenant operations of the sagas
                .MessageToDatabaseMappingConvention(context =>
                                                        {
                                                            if (context.Headers.ContainsKey("tenant"))
                                                                return context.Headers["tenant"];

                                                            return string.Empty;
                                                        });

        }
    }

    /// <summary>
    /// This mutator makes sure that the tenant id is propagated to all outgoing messages
    /// </summary>
    public class TenantPropagatingMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public IBus Bus { get; set; }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (Bus.CurrentMessageContext == null)
                return;
            if (!Bus.CurrentMessageContext.Headers.ContainsKey("tenant"))
                return;

            transportMessage.Headers["tenant"] = Bus.CurrentMessageContext.Headers["tenant"];
        }

        public void Init()
        {

            Configure.Instance.Configurer.ConfigureComponent<TenantPropagatingMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }
}
