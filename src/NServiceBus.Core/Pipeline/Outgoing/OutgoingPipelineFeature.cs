namespace NServiceBus.Features
{
    using MessageMutator;
    using Transport;

    class OutgoingPipelineFeature : Feature
    {
        public OutgoingPipelineFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var hasOutgoingMessageMutators = context.Container.HasComponent<IMutateOutgoingMessages>();
            context.Pipeline.Register("MutateOutgoingMessages", new MutateOutgoingMessageBehavior(hasOutgoingMessageMutators), "Executes IMutateOutgoingMessages");

            var hasOutgoingTransportMessageMutators = context.Container.HasComponent<IMutateOutgoingTransportMessages>();
            context.Pipeline.Register("MutateOutgoingTransportMessage", new MutateOutgoingTransportMessageBehavior(hasOutgoingTransportMessageMutators), "Executes IMutateOutgoingTransportMessages");

            context.Pipeline.Register(new AttachSenderRelatedInfoOnMessageBehavior(), "Makes sure that outgoing messages contains relevant info on the sending endpoint.");

            context.Pipeline.Register(new ForceImmediateDispatchForOperationsInSuppressedScopeBehavior(), "Detects operations performed in a suppressed scope and request them to be immediately dispatched to the transport.");

            context.Pipeline.Register(new OutgoingPhysicalToRoutingConnector(), "Starts the message dispatch pipeline");
            context.Pipeline.Register(new RoutingToDispatchConnector(), "Decides if the current message should be batched or immediately be dispatched to the transport");
            context.Pipeline.Register(new BatchToDispatchConnector(), "Passes batched messages over to the immediate dispatch part of the pipeline");
            context.Pipeline.Register(b => new ImmediateDispatchTerminator(b.Build<IDispatchMessages>()), "Hands the outgoing messages over to the transport for immediate delivery");
        }
    }
}