namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Mutates transport messages when they are received.
    /// Implementors are invoked before the logical messages have been deserialized.
    /// </summary>
    public interface IMutateIncomingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        void MutateIncoming(TransportMessage transportMessage);
    }
}