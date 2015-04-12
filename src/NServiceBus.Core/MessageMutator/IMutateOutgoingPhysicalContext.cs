namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Provides a way to mutate the context for outgoing messages in the physical stage
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateOutgoingPhysicalContext
    {
        /// <summary>
        /// Performs the mutation
        /// </summary>
        /// <param name="context">Contains the available properties that can be mutated</param>
        void MutateOutgoing(OutgoingPhysicalMutatorContext context);
    }
}