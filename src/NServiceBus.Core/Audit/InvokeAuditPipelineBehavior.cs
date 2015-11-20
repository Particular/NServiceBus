namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Audit;
    using Pipeline;
    using Transports;

    class InvokeAuditPipelineBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public InvokeAuditPipelineBehavior(IPipeInlet<AuditContext> auditPipeline, string auditAddress)
        {
            this.auditPipeline = auditPipeline;
            this.auditAddress = auditAddress;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

            var auditContext = new AuditContext(processedMessage, auditAddress, context);
            
            await auditPipeline.Put(auditContext).ConfigureAwait(false);
        }

        IPipeInlet<AuditContext> auditPipeline;
        string auditAddress;
    }
}