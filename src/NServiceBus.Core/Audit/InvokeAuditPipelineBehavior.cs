namespace NServiceBus
{
    using System;
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class InvokeAuditPipelineBehavior : PhysicalMessageProcessingStageBehavior
    {
        public InvokeAuditPipelineBehavior(PipelineBase<AuditContext> auditPipeline, string auditAddress)
        {
            this.auditPipeline = auditPipeline;
            this.auditAddress = auditAddress;
        }

        public override void Invoke(Context context, Action next)
        {
            next();

            context.GetIncomingPhysicalMessage().RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.GetIncomingPhysicalMessage().Id, context.GetIncomingPhysicalMessage().Headers, context.GetIncomingPhysicalMessage().Body);

            var auditContext = new AuditContext(context);

            context.Set(processedMessage);
            context.Set<RoutingStrategy>(new DirectToTargetDestination(auditAddress));

            auditPipeline.Invoke(auditContext);
        }

        PipelineBase<AuditContext> auditPipeline;
        string auditAddress;


        public class Registration : RegisterStep
        {
            public Registration()
                : base(WellKnownStep.AuditProcessedMessage, typeof(InvokeAuditPipelineBehavior), "Execute the audit pipeline")
            {
                InsertAfterIfExists("FirstLevelRetries");
            }
        }
    }
}