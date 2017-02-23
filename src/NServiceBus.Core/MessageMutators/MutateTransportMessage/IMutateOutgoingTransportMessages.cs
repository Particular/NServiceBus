namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Provides a way to mutate the context for outgoing messages in the physical stage.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateOutgoingTransportMessages
    {
        /// <summary>
        /// Performs the mutation.
        /// </summary>
        /// <param name="context">Contains information about the current message and provides ways to mutate it.</param>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task MutateOutgoing(MutateOutgoingTransportMessageContext context);
    }
}