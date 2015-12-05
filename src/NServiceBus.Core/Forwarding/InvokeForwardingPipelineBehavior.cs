namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class InvokeForwardingPipelineBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public InvokeForwardingPipelineBehavior(IPipelineBase<ForwardingContext> forwardingPipeline, string forwardingAddress)
        {
            this.forwardingPipeline = forwardingPipeline;
            this.forwardingAddress = forwardingAddress;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

            var forwardingContext = new ForwardingContext(processedMessage, forwardingAddress, context);

            await forwardingPipeline.Invoke(forwardingContext).ConfigureAwait(false);
        }

        IPipelineBase<ForwardingContext> forwardingPipeline;
        string forwardingAddress;
    }
}