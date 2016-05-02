namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Mutates transport messages when they are received.
    /// Implementations are invoked before the logical messages have been deserialized.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateIncomingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task MutateIncoming(MutateIncomingTransportMessageContext context);
    }
}