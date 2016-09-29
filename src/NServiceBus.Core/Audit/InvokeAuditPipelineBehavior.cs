namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

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

            var processedMessage = new OutgoingMessage(context.Message.MessageId, new Dictionary<string, string>(context.Message.Headers), context.Message.Body);

            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

            await this.Fork(auditContext).ConfigureAwait(false);
        }

        string auditAddress;
    }
}