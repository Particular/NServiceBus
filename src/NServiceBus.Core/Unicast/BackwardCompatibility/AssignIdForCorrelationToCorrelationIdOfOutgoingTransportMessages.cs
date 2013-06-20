namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class AssignIdForCorrelationToCorrelationIdOfOutgoingTransportMessages : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey("CorrId") == false)
            {
                transportMessage.Headers["CorrId"] = transportMessage.CorrelationId;
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<AssignIdForCorrelationToCorrelationIdOfOutgoingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}
