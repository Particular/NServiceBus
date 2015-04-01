namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Mutates outgoing messages
    /// </summary>
    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized
        /// </summary>
        object MutateOutgoing(object message);
    }

    /// <summary>
    /// Mutates outgoing messages
    /// </summary>
    /// Daniel: We need a better name
    public interface IMutateOutgoingMessage
    {
        /// <summary>
        /// Mutates the given message just before it's serialized
        /// </summary>
        object MutateOutgoing(object message, IMutateOutgoingMessageContext context);
    }

#pragma warning disable 1591
    public interface IMutateOutgoingMessageContext
#pragma warning restore 1591
    {
    }

    class MutateOutgoingMessageContext : IMutateOutgoingMessageContext
    {
    }
}