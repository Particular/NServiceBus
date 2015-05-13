namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Encryption;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class DecryptBehavior : LogicalMessageProcessingStageBehavior
    {
        EncryptionMutator messageMutator;

        public DecryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }
        public override async Task Invoke(Context context, Func<Task> next)
        {
            if (TransportMessageExtensions.IsControlMessage(context.Headers))
            {
                await next();
                return;
            }
            var current = context.GetLogicalMessage().Instance;
            current = messageMutator.MutateIncoming(current);
            context.GetLogicalMessage().UpdateMessageInstance(current);
            await next();
        }

        public class DecryptRegistration : RegisterStep
        {
            public DecryptRegistration()
                : base("InvokeDecryption", typeof(DecryptBehavior), "Invokes the decryption logic")
            {
                InsertBefore(WellKnownStep.MutateIncomingMessages);
            }

        }
    }
}