namespace NServiceBus.Features
{
    using NServiceBus.Pipeline;

    class OutgoingPipelineFeature : Feature
    {
        public OutgoingPipelineFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.RegisterConnector<SerializeMessageConnector>("Converts a logical message into a physical message");

            context.Pipeline.Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessagesBehavior), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingTransportMessageBehavior), "Executes IMutateOutgoingTransportMessages");

            context.Pipeline.Register("ForceImmediateDispatchForOperationsInSuppressedScopeBehavior", typeof(ForceImmediateDispatchForOperationsInSuppressedScopeBehavior), "Detects operations performed in a suppressed scope and request them to be immediately dispatched to the transport.");

            context.Pipeline.RegisterConnector<OutgoingPhysicalToRoutingConnector>("Starts the message dispatch pipeline");
            context.Pipeline.RegisterConnector<RoutingToDispatchConnector>("Decides if the current message should be batched or immediately be dispatched to the transport");
            context.Pipeline.RegisterConnector<BatchToDispatchConnector>("Passes batched messages over to the immediate dispatch part of the pipeline");
            context.Pipeline.Register("ImmediateDispatchTerminator", typeof(ImmediateDispatchTerminator), "Hands the outgoing messages over to the transport for immediate delivery");
        }
    }
}