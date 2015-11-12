namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Encryption;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using Pipeline;

    class EncryptBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        EncryptionMutator messageMutator;

        public EncryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            var currentMessageToSend = context.Message.Instance;

            currentMessageToSend = messageMutator.MutateOutgoing(currentMessageToSend);

            context.UpdateMessageInstance(currentMessageToSend);

            return next();
        }

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration()
                : base("InvokeEncryption", typeof(EncryptBehavior), "Invokes the encryption logic")
            {
                InsertAfter(WellKnownStep.MutateOutgoingMessages);
            }

        }
    }
}