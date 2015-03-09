namespace NServiceBus.MessageMutator
{
    using JetBrains.Annotations;

    /// <summary>
    /// Mutates outgoing messages
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized
        /// </summary>
        object MutateOutgoing(object message);
    }
}