namespace NServiceBus.Unicast.Monitoring
{
    using MessageMutator;
    using Messages;

    /// <summary>
    /// Mutator to set the related to header
    /// </summary>
    class CausationMutator : IMutateOutgoingTransportMessages, IConfigureBus
    {
        /// <summary>
        /// The bus is needed to get access to the current message id
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(Headers.ConversationId))
                return;

            var conversationId = CombGuid.Generate().ToString();

            if (Bus.CurrentMessageContext != null)
            {
                transportMessage.Headers[Headers.RelatedTo] = Bus.CurrentMessageContext.Id;

                string conversationIdFromCurrentMessageContext;
                if (Bus.CurrentMessageContext.Headers.TryGetValue(Headers.ConversationId, out conversationIdFromCurrentMessageContext))
                {
                    conversationId = conversationIdFromCurrentMessageContext;
                }
            }

            transportMessage.Headers[Headers.ConversationId] = conversationId;
        }

        /// <summary>
        /// Initializes 
        /// </summary>
        public void Customize(ConfigurationBuilder builder)
        {
            builder.RegisterComponents(c => c.ConfigureComponent<CausationMutator>(DependencyLifecycle.InstancePerCall));
        }
    }
}