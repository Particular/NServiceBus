namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class InvokeAuditPipelineBehavior : IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IAuditContext>
    {
        public InvokeAuditPipelineBehavior(string auditAddress)
        {
            this.auditAddress = auditAddress;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            await next(context, cancellationToken).ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, new Dictionary<string, string>(context.Message.Headers), context.Message.Body);

            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

            await this.Fork(auditContext).ConfigureAwait(false);
        }

        readonly string auditAddress;
    }
}