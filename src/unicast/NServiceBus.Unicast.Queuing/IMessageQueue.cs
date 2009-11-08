namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of a message queue
    /// </summary>
    public interface IMessageQueue
    {
        /// <summary>
        /// Initializes the message queue with the given queue used for peeking and removal, purging it as requested.
        /// </summary>
        /// <param name="inputqueue"></param>
        /// <param name="purge"></param>
        /// <param name="secondsToWaitForMessage"></param>
        void Init(string inputqueue, bool purge, int secondsToWaitForMessage);

        /// <summary>
        /// Sends the given message to the destination, flowing transactions as requested.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="destination"></param>
        /// <param name="transactional"></param>
        void Send(QueuedMessage message, string destination, bool transactional);

        /// <summary>
        /// Returns true if there's a message in the queue passed in the Init method.
        /// </summary>
        /// <returns></returns>
        bool HasMessage();

        /// <summary>
        /// Tries to receive a message from the queue passed in Init, flowing transactions as requested.
        /// </summary>
        /// <param name="transactional"></param>
        QueuedMessage Receive(bool transactional);

        /// <summary>
        /// Creates the given queue if it doesn't already exist.
        /// </summary>
        /// <param name="queue"></param>
        void CreateQueue(string queue);
    }
}
