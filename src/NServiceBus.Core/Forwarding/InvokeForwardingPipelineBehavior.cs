namespace NServiceBus
{
    using System;
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

        public override void Invoke(Context context, Action next)
        {
            next();

            context.GetIncomingPhysicalMessage().RevertToOriginalBodyIfNeeded();

            var processedMessage = new OutgoingMessage(context.GetIncomingPhysicalMessage().Id, context.GetIncomingPhysicalMessage().Headers, context.GetIncomingPhysicalMessage().Body);

            var forwardingContext = new ForwardingContext(processedMessage,context);

            context.Set<RoutingStrategy>(new DirectToTargetDestination(forwardingAddress));

            forwardingPipeline.Invoke(forwardingContext);
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