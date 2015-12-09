namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Audit;
    using Pipeline;
    using Transports;

    class InvokeAuditPipelineBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        public InvokeAuditPipelineBehavior(PipelineBase<AuditContext> auditPipeline, string auditAddress)
        {
            this.auditPipeline = auditPipeline;
            this.auditAddress = auditAddress;
        }

        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

            var auditContext = new AuditContextImpl(processedMessage, auditAddress, context);
            
            await auditPipeline.Invoke(auditContext).ConfigureAwait(false);
        }

        PipelineBase<AuditContext> auditPipeline;
        string auditAddress;
    }
}