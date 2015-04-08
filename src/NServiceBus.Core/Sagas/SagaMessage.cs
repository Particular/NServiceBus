namespace NServiceBus.Saga
{
    /// <summary>
    /// Representation of a message that is related to a saga
    /// </summary>
    public class SagaMessage
    {
        readonly bool isAllowedToStartSaga;
        readonly string messageType;

        /// <summary>
        /// Creates a new instance of <see cref="SagaMessage"/>.
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        public SagaMessage(string messageType, SagaMessageHandledBy sagaMessageHandledBy)
        {
            this.messageType = messageType;
            isAllowedToStartSaga =
                MessageHandledBy == SagaMessageHandledBy.StartedByCommand || 
                MessageHandledBy == SagaMessageHandledBy.StartedByEvent || 
                MessageHandledBy == SagaMessageHandledBy.StartedByMessage;;
        }

        /// <summary>
        /// The type of the message.
        /// </summary>
        public string MessageType
        {
            get { return messageType; }
        }

        /// <summary>
        /// The message kind indicating how the given message type is handled
        /// </summary>
        public bool IsAllowedToStartSaga
        {
            get { return isAllowedToStartSaga; }

        }
    }
}