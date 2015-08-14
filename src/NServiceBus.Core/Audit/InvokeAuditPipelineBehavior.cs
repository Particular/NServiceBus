namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
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

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next();

            context.GetPhysicalMessage().RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.GetPhysicalMessage().Id, context.GetPhysicalMessage().Headers, context.GetPhysicalMessage().Body);

            var auditContext = new AuditContext(context);

            context.Set(processedMessage);
            context.Set<RoutingStrategy>(new DirectToTargetDestination(auditAddress));

            await auditPipeline.Invoke(auditContext);
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