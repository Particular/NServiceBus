namespace NServiceBus.TransportDispatch
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class RoutingPipeline : PipelineBase<RoutingContext>
    {
        public RoutingPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) 
            : base(builder, settings, pipelineModifications)
        {
        }
    }
}