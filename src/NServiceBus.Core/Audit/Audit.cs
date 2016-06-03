namespace NServiceBus.Features
{
    using Transports;

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
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new AuditToDispatchConnector(auditConfig.TimeToBeReceived), "Dispatches the audit message to the transport");
            context.Pipeline.Register("AuditProcessedMessage", new InvokeAuditPipelineBehavior(auditConfig.Address), "Execute the audit pipeline");

            context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);
        }

        AuditConfigReader.Result auditConfig;
    }
}