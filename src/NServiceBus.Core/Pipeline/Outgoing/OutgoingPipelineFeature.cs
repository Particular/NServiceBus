namespace NServiceBus.Features
{
    using Transport;

    class OutgoingPipelineFeature : Feature
    {
        public OutgoingPipelineFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            context.Pipeline.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            context.Pipeline.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            context.Pipeline.Register(b => new ImmediateDispatchTerminator(b.Build<IDispatchMessages>()), "Hands the outgoing messages over to the transport for immediate delivery");
        }
    }
}