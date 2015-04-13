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

        /// <summary>
        /// Creates a new instance of <see cref="SagaMessage"/>.
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="isAllowedToStartSaga">Flag which indicates whether the current message type is allowed to start the saga</param>
        public SagaMessage(string messageType, bool isAllowedToStartSaga)
        {
            this.messageType = messageType;
            this.isAllowedToStartSaga = isAllowedToStartSaga;
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
        internal SagaMessageHandledBy MessageHandledBy { get; set; }
    }
}