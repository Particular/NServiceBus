namespace NServiceBus
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class BatchDispatchPipeline : PipelineBase<BatchDispatchContext>
    {
        public BatchDispatchPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) 
            : base(builder, settings, pipelineModifications)
        {
        }
    }
}