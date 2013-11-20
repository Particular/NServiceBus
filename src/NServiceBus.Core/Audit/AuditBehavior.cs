namespace NServiceBus.Audit
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class AuditBehavior : IBehavior<IncomingPhysicalMessageContext>
    {
        public MessageAuditer MessageAuditer { get; set; }

        public void Invoke(IncomingPhysicalMessageContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
        }
    }
}