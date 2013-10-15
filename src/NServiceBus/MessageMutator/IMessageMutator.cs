
namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change logical messages before any other code sees them.
    /// </summary>
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages{}

    /// <summary>
    /// Mutates incoming messages
    /// </summary>
    public interface IMutateIncomingMessages
    {
        /// <summary>
        /// Mutates the given message right after it has been deseralized
        /// </summary>
        object MutateIncoming(object message);
    }

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