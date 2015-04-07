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
        /// <param name="isAllowedToStart"><code>true</code> if the message can start the saga, <code>false</code> otherwise.</param>
        public SagaMessage(string messageType, bool isAllowedToStart)
        {
            this.messageType = messageType;
            isAllowedToStartSaga = isAllowedToStart;
        }

        /// <summary>
        /// The type of the message.
        /// </summary>
        public string MessageType
        {
            get { return messageType; }
        }

        /// <summary>
        /// True if the message can start the saga.
        /// </summary>
        public bool IsAllowedToStartSaga
        {
            get { return isAllowedToStartSaga; }
        }
    }
}