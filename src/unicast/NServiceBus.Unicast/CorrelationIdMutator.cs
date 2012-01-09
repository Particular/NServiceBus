namespace NServiceBus.Unicast
{
    using Config;
    using MessageMutator;
    using Transport;

    /// <summary>
    /// Mutator to set the correlation id
    /// </summary>
    public class CorrelationIdMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// The bus is needed to get access to the current message id
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// If no correlation id is set and the message is sent from a messagehandler the current message id
        /// will be used as correlation id to make auditing possible
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (transportMessage.CorrelationId == null && Bus.CurrentMessageContext != null)
                transportMessage.CorrelationId = Bus.CurrentMessageContext.Id;
        }

        /// <summary>
        /// Initializes 
        /// </summary>
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<CorrelationIdMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}