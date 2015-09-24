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
            context.Pipeline.RegisterConnector<PublishToOutgoingContextConnector>("Connect the publish stage to the outgoing stage");
            context.Pipeline.RegisterConnector<SendToOutgoingContextConnector>("Connect the send stage to the outgoing stage");
            context.Pipeline.RegisterConnector<ReplyToOutgoingContextConnector>("Connect the reply stage to the outgoing stage");
            context.Pipeline.RegisterConnector<SerializeMessagesBehavior>("Converts a logical message into a physical message");

            context.Pipeline.Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingTransportMessageBehavior), "Executes IMutateOutgoingTransportMessages");

            context.Pipeline.RegisterConnector<DispatchMessageToTransportConnector>("Starts the message dispatch pipeline");
            context.Pipeline.RegisterConnector<BatchOrImmediateDispatchConnector>("Decides if the current message should be batched or immediately be dispatched to the transport");
            context.Pipeline.RegisterConnector<BatchToImmediateDispatchConnector>("Passes batched messages over to the immediate dispatch part of the pipeline");
            context.Pipeline.Register("ImmediateDispatchTerminator", typeof(ImmediateDispatchTerminator), "Hands the outgoing messages over to the transport for immediate delivery");
        }
    }
}