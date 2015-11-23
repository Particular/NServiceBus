namespace NServiceBus.Features
{
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
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(WellKnownStep.AuditProcessedMessage, typeof(InvokeAuditPipelineBehavior), "Execute the audit pipeline");
            context.Pipeline.RegisterConnector<AuditToRoutingConnector>("Dispatches the audit message to the transport");
            context.Container.ConfigureComponent(b => new AuditPipeline(b, context.Settings, context.Settings.Get<PipelineConfiguration>().MainPipeline), DependencyLifecycle.SingleInstance);            

            context.Container.ConfigureComponent(b =>
            {
                var auditPipeline = b.Build<IPipeInlet<AuditContext>>();
                return new InvokeAuditPipelineBehavior(auditPipeline, auditConfig.Address);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b => new AuditToRoutingConnector(auditConfig.TimeToBeReceived), DependencyLifecycle.SingleInstance);
            context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);
        }

        AuditConfigReader.Result auditConfig;
    }
}