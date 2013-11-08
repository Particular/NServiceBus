namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using Audit;

    class AuditBehavior : IBehavior<PhysicalMessageContext>
    {
        public MessageAuditer MessageAuditer { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
        }
    }
}