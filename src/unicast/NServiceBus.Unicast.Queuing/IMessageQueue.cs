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
        /// Peeks into the queue passed in Init, and returning a message if one is there.
        /// </summary>
        /// <returns></returns>
        QueuedMessage Peek();

        /// <summary>
        /// Removes the given message from the queue passed in Init, flowing transactions as requested.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="transactional"></param>
        void RemoveQueuedMessage(QueuedMessage message, bool transactional);

        /// <summary>
        /// Creates the given queue if it doesn't already exist.
        /// </summary>
        /// <param name="queue"></param>
        void CreateQueue(string queue);
    }
}
