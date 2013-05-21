namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class AssignIdForCorrelationToCorrelationIdOfOutgoingTransportMessages : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers["CorrId"] = transportMessage.CorrelationId;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<AssignIdForCorrelationToCorrelationIdOfOutgoingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}