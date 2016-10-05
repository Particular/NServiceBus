namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class InvokeForwardingPipelineBehavior : IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IForwardingContext>
    {
        public InvokeForwardingPipelineBehavior(string forwardingAddress)
        {
            this.forwardingAddress = forwardingAddress;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            await next(context).ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

            var forwardingContext = this.CreateForwardingContext(processedMessage, forwardingAddress, context);

            await this.Fork(forwardingContext).ConfigureAwait(false);
        }

        string forwardingAddress;
    }
}