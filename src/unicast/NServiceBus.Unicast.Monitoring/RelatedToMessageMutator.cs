namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;
    using NServiceBus.Config;
    using Transport;

    /// <summary>
    /// Mutator to set the related to header
    /// </summary>
    public class RelatedToMessageMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// The bus is needed to get access to the current message id
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (Bus.CurrentMessageContext != null)
                transportMessage.Headers[Headers.RelatedTo] = Bus.CurrentMessageContext.Id;
        }

        /// <summary>
        /// Initializes 
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<RelatedToMessageMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}