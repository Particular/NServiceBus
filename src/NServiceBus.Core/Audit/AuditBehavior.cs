namespace NServiceBus.Audit
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;


    class AuditBehavior : IBehavior<IncomingContext>
    {
        public MessageAuditer MessageAuditer { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
        }
    }
}