namespace NServiceBus.MessageMutator
{
    using JetBrains.Annotations;

    /// <summary>
    /// Provides a way to mutate the context for outgoing messages in the physical stage
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateOutgoingPhysicalMessages
    {
        /// <summary>
        /// Performs the mutation
        /// </summary>
        /// <param name="context">Contains the available properties that can be mutated</param>
        void MutateOutgoing(MutateOutgoingPhysicalMessageContext context);
    }
}