namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class MutateIdOfOutgoingTransportMessages : IMutateOutgoingTransportMessages, INeedInitialization
    {
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (!transportMessage.Headers.ContainsKey("CorrId"))
            {
                transportMessage.Headers["CorrId"] = transportMessage.Id;
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateIdOfOutgoingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}