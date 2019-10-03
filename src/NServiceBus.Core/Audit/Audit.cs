namespace NServiceBus.Features
{
    using Logging;
    using Transport;

    /// <summary>
    /// Enabled message auditing for this endpoint.
    /// </summary>
    public class Audit : Feature
    {
        internal Audit()
        {
            EnableByDefault();
            Defaults(settings =>
            {
                settings.Set(AuditConfigReader.GetConfiguredAuditQueue(settings));
            });
            Prerequisite(config => config.Settings.GetOrDefault<AuditConfigReader.Result>() != null, "No configured audit queue was found");
        }


        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var auditConfig = context.Settings.Get<AuditConfigReader.Result>();

            context.Pipeline.Register("AuditToDispatchConnector", new AuditToRoutingConnector(auditConfig.TimeToBeReceived), "Dispatches the audit message to the transport");
            context.Pipeline.Register("AuditProcessedMessage", new InvokeAuditPipelineBehavior(auditConfig.Address), "Execute the audit pipeline");

            context.Settings.Get<QueueBindings>().BindSending(auditConfig.Address);

            context.Settings.AddStartupDiagnosticsSection("Audit", new
            {
                AuditQueue = auditConfig.Address,
                AuditTTBR = auditConfig.TimeToBeReceived?.ToString("g") ?? "-"
            });

            logger.InfoFormat($"Auditing processed messages to '{auditConfig.Address}'");
        }

        static readonly ILog logger = LogManager.GetLogger<Audit>();
    }
}