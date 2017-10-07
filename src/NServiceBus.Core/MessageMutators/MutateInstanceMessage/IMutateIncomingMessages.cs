namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;

    /// <summary>
    /// Mutates incoming messages.
    /// </summary>
    public interface IMutateIncomingMessages
    {
        /// <summary>
        /// Mutates the given message right after it has been deserialized.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task MutateIncoming(MutateIncomingMessageContext context);
    }
}