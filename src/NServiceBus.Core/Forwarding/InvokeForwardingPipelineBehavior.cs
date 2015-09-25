namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Forwarding;
    using Pipeline;
    using Routing;
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

            context.Message.RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.Message.Id, context.Message.Headers, context.Message.Body);

            var forwardingContext = new ForwardingContext(processedMessage,context);

            context.Set<RoutingStrategy>(new DirectToTargetDestination(forwardingAddress));

            await forwardingPipeline.Invoke(forwardingContext).ConfigureAwait(false);
        }

        IPipelineBase<ForwardingContext> forwardingPipeline;
        string forwardingAddress;
    }
}