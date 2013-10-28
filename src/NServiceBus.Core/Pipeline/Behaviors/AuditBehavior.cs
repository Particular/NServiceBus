namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using Audit;

    class AuditBehavior : IBehavior
    {
        public MessageAuditer MessageAuditer { get; set; }
        public void Invoke(BehaviorContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.TransportMessage);
        }
    }
}