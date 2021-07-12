namespace NServiceBus
{
    using System;
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

            var headers = MainPipelineExecutor.HeaderPool.Get();
            context.Message.Headers.CopyTo(headers);

            try
            {
                var processedMessage = new OutgoingMessage(context.Message.MessageId, headers, context.Message.Body);

                var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

                await this.Fork(auditContext).ConfigureAwait(false);
            }
            finally
            {
                MainPipelineExecutor.HeaderPool.Return(headers);
            }
        }

        readonly string auditAddress;
    }
}