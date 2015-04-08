namespace NServiceBus.Saga
{
    using NServiceBus.Sagas;

    /// <summary>
    /// Representation of a message that is related to a saga
    /// </summary>
    public class SagaMessage
    {
        readonly bool isAllowedToStartSaga;
        readonly string messageType;
        readonly SagaMessageHandledBy messageHandledBy;

        /// <summary>
        /// Creates a new instance of <see cref="SagaMessage"/>.
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="sagaMessageHandledBy">Meta information about how a given saga message is handled on the saga</param>
        public SagaMessage(string messageType, SagaMessageHandledBy sagaMessageHandledBy)
        {
            this.messageType = messageType;
            messageHandledBy = sagaMessageHandledBy;
            isAllowedToStartSaga =
                messageHandledBy == SagaMessageHandledBy.StartedByCommand ||
                messageHandledBy == SagaMessageHandledBy.StartedByEvent ||
                messageHandledBy == SagaMessageHandledBy.StartedByMessage;
        }

        /// <summary>
        /// The type of the message.
        /// </summary>
        public string MessageType
        {
            get { return messageType; }
        }

        /// <summary>
        /// true when the message is starting a saga; otherwise false.
        /// </summary>
        public bool IsAllowedToStartSaga
        {
            get { return isAllowedToStartSaga; }
        }

        /// <summary>
        /// The message kind indicating how the given message type is handled
        /// </summary>
        public SagaMessageHandledBy MessageHandledBy
        {
            get { return messageHandledBy; }
        }
    }
}