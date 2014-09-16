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
}