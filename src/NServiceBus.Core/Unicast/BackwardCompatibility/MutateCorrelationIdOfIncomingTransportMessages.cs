namespace NServiceBus.Unicast.BackwardCompatibility
{
    using NServiceBus.MessageMutator;

    public class MutateCorrelationIdOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Unsure that the IdForCorrelation header is copied over to the correlation id when set.
        /// </summary>
        /// <param name="transportMessage">Transport Message to mutate.</param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(Headers.IdForCorrelation))
            {
                transportMessage.CorrelationId = transportMessage.Headers[Headers.IdForCorrelation];
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateCorrelationIdOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}