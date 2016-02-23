namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class EncryptBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        EncryptionMutator messageMutator;

        public EncryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var currentMessageToSend = context.Message.Instance;

            currentMessageToSend = messageMutator.MutateOutgoing(currentMessageToSend);

            context.UpdateMessage(currentMessageToSend);

            return next();
        }

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration(EncryptionMutator mutator)
                : base("InvokeEncryption", typeof(EncryptBehavior), "Invokes the encryption logic", b => new EncryptBehavior(mutator))
            {
                InsertAfter(WellKnownStep.MutateOutgoingMessages);
            }

        }
    }
}