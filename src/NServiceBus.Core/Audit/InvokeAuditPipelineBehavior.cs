#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;
using Transport;
using NServiceBus.Utils;

class InvokeAuditPipelineBehavior : IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IAuditContext>
{
    public InvokeAuditPipelineBehavior(string auditAddress, TimeSpan? timeToBeReceived)
    {
        this.auditAddress = auditAddress;
        this.timeToBeReceived = timeToBeReceived;
    }

    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        await next(context).ConfigureAwait(false);

        context.Message.RevertToOriginalBodyIfNeeded();

        var auditHeaders = HeaderPool.Shared.Rent(context.Message.Headers.Count);
        context.Message.Headers.CopyTo(auditHeaders);
        var processedMessage = new OutgoingMessage(context.Message.MessageId, auditHeaders, context.Message.Body);

        var auditContext = this.CreateAuditContext(processedMessage, auditAddress, timeToBeReceived, context);

        await this.Fork(auditContext).ConfigureAwait(false);
    }

    readonly string auditAddress;
    readonly TimeSpan? timeToBeReceived;
}