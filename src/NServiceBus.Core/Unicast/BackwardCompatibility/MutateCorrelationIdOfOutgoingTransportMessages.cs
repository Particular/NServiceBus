namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class MutateCorrelationIdOfOutgoingTransportMessages : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (!transportMessage.Headers.ContainsKey(Headers.IdForCorrelation))
            {
                transportMessage.Headers[Headers.IdForCorrelation] = transportMessage.CorrelationId;
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateCorrelationIdOfOutgoingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}