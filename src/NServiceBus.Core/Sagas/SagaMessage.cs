namespace NServiceBus.Saga
{
    /// <summary>
    /// Representation of a message that is related to a saga
    /// </summary>
    public class SagaMessage
    {
        /// <summary>
        /// True if the message can start the saga
        /// </summary>
        public readonly bool IsAllowedToStartSaga;

        /// <summary>
        /// The type of the message
        /// </summary>
        public readonly string MessageType;

        internal SagaMessage(string messageType, bool isAllowedToStart)
        {
            MessageType = messageType;
            IsAllowedToStartSaga = isAllowedToStart;
        }
    }
}