namespace NServiceBus.Features
{
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
            context.Container.ConfigureComponent(() => new InvokeAuditPipelineBehavior(auditConfig.Address), DependencyLifecycle.InstancePerCall);

            context.Pipeline.RegisterConnector<AuditToDispatchConnector>("Dispatches the audit message to the transport");
            context.Pipeline.Register(WellKnownStep.AuditProcessedMessage, typeof(InvokeAuditPipelineBehavior), "Execute the audit pipeline");

            context.Container.ConfigureComponent(b => new AuditToDispatchConnector(auditConfig.TimeToBeReceived), DependencyLifecycle.SingleInstance);
            context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);
        }

        AuditConfigReader.Result auditConfig;
    }
}