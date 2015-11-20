namespace NServiceBus.Audit
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class AuditPipeline : PipelineBase<AuditContext>
    {
        public AuditPipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications) 
            : base(builder, settings, pipelineModifications)
        {
        }
    }
}