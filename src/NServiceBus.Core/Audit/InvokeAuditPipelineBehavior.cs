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
            var originalMessageBody = context.Message.Body;

            await next(context).ConfigureAwait(false);

            //TODO consider change the Body type of OutgoingMessage as well
            var processedMessage = new OutgoingMessage(context.Message.MessageId, new Dictionary<string, string>(context.Message.Headers), originalMessageBody.CreateCopy());

            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

            await this.Fork(auditContext).ConfigureAwait(false);
        }

        readonly string auditAddress;
    }
}