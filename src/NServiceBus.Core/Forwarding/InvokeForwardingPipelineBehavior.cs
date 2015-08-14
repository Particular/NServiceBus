namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Forwarding;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class InvokeForwardingPipelineBehavior : PhysicalMessageProcessingStageBehavior
    {
        public InvokeForwardingPipelineBehavior(IPipelineBase<ForwardingContext> forwardingPipeline, string forwardingAddress)
        {
            this.forwardingPipeline = forwardingPipeline;
            this.forwardingAddress = forwardingAddress;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next();

            context.GetPhysicalMessage().RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.GetPhysicalMessage().Id, context.GetPhysicalMessage().Headers, context.GetPhysicalMessage().Body);

            var forwardingContext = new ForwardingContext(processedMessage,context);

            context.Set<RoutingStrategy>(new DirectToTargetDestination(forwardingAddress));

            await forwardingPipeline.Invoke(forwardingContext);
        }

        IPipelineBase<ForwardingContext> forwardingPipeline;
        string forwardingAddress;


        public class Registration : RegisterStep
        {
            public Registration()
                : base("InvokeForwardingPipeline", typeof(InvokeForwardingPipelineBehavior), "Execute the forwarding pipeline")
            {
                InsertAfterIfExists("FirstLevelRetries");
            }
        }
    }
}