namespace NServiceBus.Routing
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class UnsubscribePipeline : PipelineBase<UnsubscribeContext>
    {
        public UnsubscribePipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) : base(builder, settings, pipelineModifications)
        {
        }
    }
}