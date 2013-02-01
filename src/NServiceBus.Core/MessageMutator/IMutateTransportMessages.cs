
namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change transport messages before any other code sees them. 
    /// </summary>
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages {}

    /// <summary>
    /// Mutates transport messages before they are sent.
    /// Implementors are invoked after the logical messages have been serialized.
    /// </summary>
    public interface IMutateOutgoingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        void MutateOutgoing(object[] messages, TransportMessage transportMessage);
    }

    /// <summary>
    /// Mutates transport messages when they are received.
    /// Implementors are invoked before the logical messages have been deserialized.
    /// </summary>
    public interface IMutateIncomingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        /// <param name="transportMessage"></param>
        void MutateIncoming(TransportMessage transportMessage);
    }
}
