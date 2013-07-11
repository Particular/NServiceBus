namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;

    public class VersionMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[NServiceBus.Headers.NServiceBusVersion] = NServiceBus.NServiceBusVersion.Version;
        }
     
        /// <summary>
        /// Initializer
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<VersionMutator>(DependencyLifecycle.SingleInstance);
        }
    }
}