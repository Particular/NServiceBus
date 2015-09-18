namespace NServiceBus.Features
{
    using NServiceBus.Pipeline;

    class ReceiveFeature : Feature
    {
        public ReceiveFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                var pipeline = new PipelineBase<BatchDispatchContext>(b, context.Settings, pipelinesCollection.MainPipeline);
     
                return new TransportReceiveToPhysicalMessageProcessingConnector(pipeline);
            }, DependencyLifecycle.InstancePerCall);
        }
    }
}