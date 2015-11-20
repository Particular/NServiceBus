namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class SendPipeline : PipelineBase<OutgoingSendContext>
    {
        public SendPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) : base(builder, settings, pipelineModifications)
        {
        }
    }
}