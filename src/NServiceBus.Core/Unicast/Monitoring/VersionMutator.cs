namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;

    [ObsoleteEx(TreatAsErrorFromVersion = "4.4", RemoveInVersion = "5.0")]
    public class VersionMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
           
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