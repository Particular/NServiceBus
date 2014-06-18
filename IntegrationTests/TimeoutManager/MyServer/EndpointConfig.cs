using NServiceBus.Persistence;
using NServiceBus.Unicast.Messages;

namespace MyServer
{
    using NServiceBus;
    using NServiceBus.MessageMutator;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,INeedInitialization
    {
        public void Init(Configure config)
        {
            config.UsePersistence<NServiceBus.Persistence.Legacy.Msmq>();
            config.UsePersistence<InMemory>();
            
        }
    }

    /// <summary>
    /// This mutator makes sure that the tenant id is propagated to all outgoing messages
    /// </summary>
    public class TenantPropagatingMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public IBus Bus { get; set; }

        
        public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
        {
            if (Bus.CurrentMessageContext == null)
                return;
            if (!Bus.CurrentMessageContext.Headers.ContainsKey("tenant"))
                return;

            transportMessage.Headers["tenant"] = Bus.CurrentMessageContext.Headers["tenant"];
        }

        public void Init(Configure config)
        {

            config.Configurer.ConfigureComponent<TenantPropagatingMutator>(
                DependencyLifecycle.InstancePerCall);
        }

    }
}
