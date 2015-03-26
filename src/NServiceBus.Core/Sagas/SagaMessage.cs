namespace NServiceBus.Sagas
{
    /// <summary>
    /// Representation of a message that is related to a saga
    /// </summary>
    class SagaMessage
    {
        /// <summary>
        /// True if the message can start the saga
        /// </summary>
        public readonly bool IsAllowedToStartSaga;

        /// <summary>
        /// The type of the message
        /// </summary>
        public readonly string MessageType;

        /// <summary>
        /// The message kind indicating how the given message type is handled
        /// </summary>
        public readonly SagaMessageHandledBy MessageHandledBy;

        internal SagaMessage(string messageType, SagaMessageHandledBy sagaMessageHandledBy)
        {
            MessageType = messageType;
            MessageHandledBy = sagaMessageHandledBy;
            IsAllowedToStartSaga =
                MessageHandledBy.HasFlag(SagaMessageHandledBy.StartedByMessage) || 
                MessageHandledBy.HasFlag(SagaMessageHandledBy.StartedByConsumedEvent) || 
                MessageHandledBy.HasFlag(SagaMessageHandledBy.StartedByMessage);
        }
    }
}