namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;

    /// <summary>
    /// Mutates outgoing messages.
    /// </summary>
    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task MutateOutgoing(MutateOutgoingMessageContext context);
    }
}