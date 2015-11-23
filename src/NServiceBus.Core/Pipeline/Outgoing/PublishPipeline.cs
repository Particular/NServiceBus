namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class PublishPipeline : PipelineBase<OutgoingPublishContext>
    {
        public PublishPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) 
            : base(builder, settings, pipelineModifications)
        {
        }
    }
}