
namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to hook into the seralization pipeline
    /// </summary>
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages{}

    /// <summary>
    /// Mutaor for incoming messages
    /// </summary>
    public interface IMutateIncomingMessages
    {
        /// <summary>
        /// Mutates the given message right after it has been deseralized
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        IMessage MutateIncoming(IMessage message);
    }

    /// <summary>
    /// Mutates outgoing messages
    /// </summary>
    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        IMessage MutateOutgoing(IMessage message);
    }
}