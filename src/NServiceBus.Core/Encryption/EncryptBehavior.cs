﻿namespace NServiceBus
{
    using System;
    using NServiceBus.Encryption;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class EncryptBehavior : IBehavior<OutgoingContext>
    {
        EncryptionMutator messageMutator;

        public EncryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }

        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            var currentMessageToSend = context.OutgoingLogicalMessage.Instance;
            currentMessageToSend = messageMutator.MutateOutgoing(currentMessageToSend);
            context.OutgoingLogicalMessage.UpdateMessageInstance(currentMessageToSend);
            next();
        }

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration()
                : base("InvokeEncryption", typeof(EncryptBehavior), "Invokes the encryption logic")
            {
                InsertAfter(WellKnownStep.MutateOutgoingMessages);
                InsertBefore(WellKnownStep.CreatePhysicalMessage);
            }

        }
    }
}