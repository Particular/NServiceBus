namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

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

            var processedMessage = context.Message.ToOutgoingMessage();
            
            var forwardingContext = this.CreateForwardingContext(processedMessage, forwardingAddress, context);

            await this.Fork(forwardingContext).ConfigureAwait(false);
        }

        string forwardingAddress;
    }
}