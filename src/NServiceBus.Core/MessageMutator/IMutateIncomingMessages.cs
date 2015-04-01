namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Mutates incoming messages
    /// </summary>
    public interface IMutateIncomingMessages
    {
        /// <summary>
        /// Mutates the given message right after it has been deserialized
        /// </summary>
        object MutateIncoming(object message);
    }

    /// <summary>
    /// Mutates incoming messages
    /// </summary>
    /// Daniel: We need a better name
    public interface IMutateIncomingMessage
    {
        /// <summary>
        /// Mutates the given message right after it has been deserialized
        /// </summary>
        object MutateIncoming(object message, IMutateIncomingMessageContext context);
    }

#pragma warning disable 1591
    public interface IMutateIncomingMessageContext
#pragma warning restore 1591
    {
    }

    class MutateIncomingMessageContext : IMutateIncomingMessageContext
    {
    }
}