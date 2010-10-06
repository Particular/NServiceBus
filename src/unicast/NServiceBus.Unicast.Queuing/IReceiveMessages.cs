using NServiceBus.Unicast.Transport;
namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of a the capability to receive messages
    /// </summary>
    public interface IReceiveMessages
    {
        /// <summary>
        /// Initializes the message queue.
        /// </summary>
        /// <param name="inputqueue"></param>
        void Init(string inputqueue);

        /// <summary>
        /// Returns true if there's a message in the queue passed in the Init method.
        /// </summary>
        /// <returns></returns>
        bool HasMessage();

        /// <summary>
        /// Tries to receive a message from the queue passed in Init, flowing transactions as requested.
        /// </summary>
        /// <param name="transactional"></param>
        TransportMessage Receive(bool transactional);
    }
}
