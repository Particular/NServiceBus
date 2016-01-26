namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class DecryptBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        EncryptionMutator messageMutator;

        public DecryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            if (TransportMessageExtensions.IsControlMessage(context.Headers))
            {
                await next().ConfigureAwait(false);
                return;
            }
            var current = context.Message.Instance;
            current = messageMutator.MutateIncoming(current);
            context.Message.UpdateMessageInstance(current);

            await next().ConfigureAwait(false);
        }

        public class DecryptRegistration : RegisterStep
        {
            public DecryptRegistration(EncryptionMutator mutator)
                : base("InvokeDecryption", typeof(DecryptBehavior), "Invokes the decryption logic", b => new DecryptBehavior(mutator))
            {
                InsertBefore(WellKnownStep.MutateIncomingMessages);
            }

        }
    }
}