namespace NServiceBus.Audit
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AuditBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public MessageAuditer MessageAuditer { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();
            MessageAuditer.ForwardMessageToAuditQueue(context.PhysicalMessage);
        }
    }
}