namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    class ReceivePipeline : PipelineBase<TransportReceiveContext>
    {
        public ReceivePipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) 
            : base(builder, settings, pipelineModifications)
        {
        }
    }
}