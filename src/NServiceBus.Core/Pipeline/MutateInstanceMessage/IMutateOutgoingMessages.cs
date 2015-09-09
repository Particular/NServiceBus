namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Mutates outgoing messages.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateOutgoingMessages
    {
        /// <summary>
        /// Mutates the given message just before it's serialized.
        /// </summary>
        Task MutateOutgoing(MutateOutgoingMessageContext context);
    }
}