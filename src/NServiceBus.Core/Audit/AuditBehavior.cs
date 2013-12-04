namespace NServiceBus.Audit
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class AuditBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public MessageAuditer MessageAuditer { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
        }
    }
}