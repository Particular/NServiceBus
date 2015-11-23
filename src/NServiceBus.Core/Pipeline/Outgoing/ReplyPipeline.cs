namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class ReplyPipeline : PipelineBase<OutgoingReplyContext>
    {
        public ReplyPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) : base(builder, settings, pipelineModifications)
        {
        }
    }
}