namespace NServiceBus.MessageMutator
{
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
        void MutateOutgoing(MutateOutgoingTransportMessagesContext context);
    }
}