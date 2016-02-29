namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class InvokeForwardingPipelineBehavior : ForkConnector<IIncomingPhysicalMessageContext, IForwardingContext>
    {
        public InvokeForwardingPipelineBehavior(string forwardingAddress)
        {
            this.forwardingAddress = forwardingAddress;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next, Func<IForwardingContext, Task> fork)
        {
            await next().ConfigureAwait(false);

            context.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

            var forwardingContext = this.CreateForwardingContext(processedMessage, forwardingAddress, context);

            await fork(forwardingContext).ConfigureAwait(false);
        }

        string forwardingAddress;
    }
}