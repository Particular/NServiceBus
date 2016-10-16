namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class InvokeAuditPipelineBehavior : IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IAuditContext>
    {
        public InvokeAuditPipelineBehavior(string auditAddress)
        {
            this.auditAddress = auditAddress;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            await next(context).ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = context.Message.ToOutgoingMessage();
            
            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

            await this.Fork(auditContext).ConfigureAwait(false);
        }

        string auditAddress;
    }
}