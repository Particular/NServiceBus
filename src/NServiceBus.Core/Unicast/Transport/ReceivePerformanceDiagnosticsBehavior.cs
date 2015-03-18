namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast.Transport.Monitoring;

    class ReceivePerformanceDiagnosticsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            context.Get<ReceivePerformanceDiagnostics>().MessageDequeued();
            next();
        }
    }
}