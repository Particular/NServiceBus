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
    /// <summary>
    /// Mutates transport messages when they are received.
    /// Implementors are invoked before the logical messages have been deserialized.
    /// </summary>
    ///  Daniel: We need a better name
    public interface IMutateIncomingTransportMessage
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        void MutateIncoming(TransportMessage transportMessage, IMutateIncomingTransportMessageContext context);
    }

#pragma warning disable 1591
    public interface IMutateIncomingTransportMessageContext
#pragma warning restore 1591
    {
    }

    class MutateIncomingTransportMessageContext : IMutateIncomingTransportMessageContext
    {
    }
}