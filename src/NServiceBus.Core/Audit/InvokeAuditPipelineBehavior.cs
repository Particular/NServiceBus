namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Audit;
    using Pipeline;
    using Transports;

    class InvokeAuditPipelineBehavior : PhysicalMessageProcessingStageBehavior
    {
        public InvokeAuditPipelineBehavior(PipelineBase<AuditContext> auditPipeline, string auditAddress)
        {
            this.auditPipeline = auditPipeline;
            this.auditAddress = auditAddress;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            // TODO: How to enable this?
            // context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.BodyStream);

            var auditContext = new AuditContext(processedMessage, auditAddress, context);
            
            await auditPipeline.Invoke(auditContext).ConfigureAwait(false);
        }

        PipelineBase<AuditContext> auditPipeline;
        string auditAddress;
    }
}