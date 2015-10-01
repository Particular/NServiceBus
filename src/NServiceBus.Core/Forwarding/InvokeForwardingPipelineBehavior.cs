namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Forwarding;
    using Pipeline;
    using Transports;

    class InvokeForwardingPipelineBehavior : PhysicalMessageProcessingStageBehavior
    {
        public InvokeForwardingPipelineBehavior(IPipelineBase<ForwardingContext> forwardingPipeline, string forwardingAddress)
        {
            this.forwardingPipeline = forwardingPipeline;
            this.forwardingAddress = forwardingAddress;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            // TODO: How to enable this/
            // context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.BodyStream);

            var forwardingContext = new ForwardingContext(processedMessage, forwardingAddress, context);

            await forwardingPipeline.Invoke(forwardingContext).ConfigureAwait(false);
        }

        IPipelineBase<ForwardingContext> forwardingPipeline;
        string forwardingAddress;
    }
}