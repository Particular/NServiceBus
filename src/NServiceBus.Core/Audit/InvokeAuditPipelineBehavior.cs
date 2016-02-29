﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class InvokeAuditPipelineBehavior : ForkConnector<IIncomingPhysicalMessageContext, IAuditContext>
    {
        public InvokeAuditPipelineBehavior(string auditAddress)
        {
            this.auditAddress = auditAddress;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next, Func<IAuditContext, Task> fork)
        {
            await next().ConfigureAwait(false);

            context.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);
            
            await fork(auditContext).ConfigureAwait(false);
        }

        string auditAddress;
    }
}