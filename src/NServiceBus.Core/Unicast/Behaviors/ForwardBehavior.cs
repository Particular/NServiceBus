﻿namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast;
    using Pipeline;
    using Transports;

    class ForwardBehavior : PhysicalMessageProcessingStageBehavior
    {
        public IAuditMessages MessageAuditer { get; set; }

        public string ForwardReceivedMessagesTo { get; set; }


        public override void Invoke(Context context, Action next)
        {
            next();

            context.PhysicalMessage.RevertToOriginalBodyIfNeeded();

            MessageAuditer.Audit(new SendOptions(ForwardReceivedMessagesTo), new OutgoingMessage(context.PhysicalMessage.Id,context.PhysicalMessage.Headers,context.PhysicalMessage.Body));
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to the specified queue in the UnicastBus config section.")
            {
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}