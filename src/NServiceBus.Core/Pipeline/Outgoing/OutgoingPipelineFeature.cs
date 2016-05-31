namespace NServiceBus.Features
{
    class OutgoingPipelineFeature : Feature
    {
        public OutgoingPipelineFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("MutateOutgoingMessages", typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register("MutateOutgoingTransportMessage", typeof(MutateOutgoingTransportMessageBehavior), "Executes IMutateOutgoingTransportMessages");

            context.Pipeline.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");

            context.Pipeline.Register(new ForceImmediateDispatchForOperationsInSuppressedScopeBehavior(), "Detects operations performed in a suppressed scope and request them to be immediately dispatched to the transport.");

            context.Pipeline.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            context.Pipeline.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            context.Pipeline.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            context.Pipeline.Register(typeof(ImmediateDispatchTerminator), "Hands the outgoing messages over to the transport for immediate delivery");
        }
    }
}