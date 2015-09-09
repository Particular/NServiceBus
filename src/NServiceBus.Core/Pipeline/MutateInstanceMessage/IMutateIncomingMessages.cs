namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Mutates incoming messages.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateIncomingMessages
    {
        /// <summary>
        /// Mutates the given message right after it has been deserialized.
        /// </summary>
        Task MutateIncoming(MutateIncomingMessageContext context);
    }
}