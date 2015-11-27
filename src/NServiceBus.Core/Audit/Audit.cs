namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Enabled message auditing for this endpoint.
    /// </summary>
    public class Audit : Feature
    {
        internal Audit()
        {
            EnableByDefault();
            Prerequisite(config => AuditConfigReader.GetConfiguredAuditQueue(config.Settings, out auditConfig), "No configured audit queue was found");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(WellKnownStep.AuditProcessedMessage, typeof(InvokeAuditPipelineBehavior), "Execute the audit pipeline");
            context.Pipeline.RegisterConnector<AuditToDispatchConnector>("Dispatches the audit message to the transport");
         
            context.Container.ConfigureComponent(b =>
            {
                var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();
                var auditPipeline = new PipelineBase<AuditContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                return new InvokeAuditPipelineBehavior(auditPipeline,auditConfig.Address);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b => new AuditToDispatchConnector(auditConfig.TimeToBeReceived), DependencyLifecycle.SingleInstance);
            context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);

            return FeatureStartupTask.None;
        }

        AuditConfigReader.Result auditConfig;
    }
}