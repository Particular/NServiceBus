namespace NServiceBus.Routing
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class SubscribePipeline : PipelineBase<SubscribeContext>
    {
        public SubscribePipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) : base(builder, settings, pipelineModifications)
        {
        }
    }
}