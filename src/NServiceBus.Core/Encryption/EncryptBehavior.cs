namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Encryption;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class EncryptBehavior : Behavior<OutgoingContext>
    {
        EncryptionMutator messageMutator;

        public EncryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }

        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            var currentMessageToSend = context.GetMessageInstance();

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