namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of a the capability to receive messages
    /// </summary>
    public interface IReceiveMessages
    {
        /// <summary>
        /// Initializes the message receiver.
        /// </summary>
        /// <param name="address">The address of the message source</param>
        /// <param name="transactional">Indicates if the receiver should be transactional</param>
        void Init(Address address, bool transactional);

        /// <summary>
        /// Returns true if there's a message ready to be received at the address passed in the Init method.
        /// </summary>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        bool HasMessage();

        /// <summary>
        /// Tries to receive a message from the address passed in Init.
        /// </summary>
        /// <returns>The first transport message available. If no message is present null will be returned</returns>
        TransportMessage Receive();
    }
}
