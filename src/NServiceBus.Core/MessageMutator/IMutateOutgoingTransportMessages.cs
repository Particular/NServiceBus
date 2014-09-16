namespace NServiceBus.MessageMutator
{
    using Unicast.Messages;

    /// <summary>
    /// Mutates transport messages before they are sent.
    /// Implementors are invoked after the logical messages have been serialized.
    /// </summary>
    public interface IMutateOutgoingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        /// <remarks>Mutations should be applied to the <paramref name="transportMessage"/>.</remarks>
        /// <param name="logicalMessage">The outgoing <see cref="LogicalMessage"/> that wraps the actual business message. See <see cref="LogicalMessage.Instance"/> to get the actual business message.</param>
        /// <param name="transportMessage">The physical message about to be sent to the queue.</param>
        void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage);
    }
}