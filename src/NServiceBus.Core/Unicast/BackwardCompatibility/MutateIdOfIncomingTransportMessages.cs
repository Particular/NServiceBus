namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class MutateIdOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Unsure that the IdForCorrelation header is copied over to the correlation id when set.
        /// </summary>
        /// <param name="transportMessage">Transport Message to mutate.</param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey("CorrId"))
            {
                transportMessage.Id = transportMessage.Headers["CorrId"];
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateIdOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}