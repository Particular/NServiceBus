namespace NServiceBus.MessageMutator
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Mutates transport messages when they are received.
    /// Implementors are invoked before the logical messages have been deserialized.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMutateIncomingTransportMessages
    {
        /// <summary>
        /// Modifies various properties of the transport message.
        /// </summary>
        Task MutateIncoming(MutateIncomingTransportMessageContext context);
    }
}