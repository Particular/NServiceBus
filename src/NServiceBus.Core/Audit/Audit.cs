namespace NServiceBus.Features
{
    using NServiceBus.Unicast.Queuing.Installers;

    /// <summary>
    /// Enabled message auditing for this endpoint.
    /// </summary>
    public class Audit : Feature
    {
        internal Audit()
        {
            EnableByDefault();
            Prerequisite(config =>
                AuditConfigReader.GetConfiguredAuditQueue(config.Settings, out auditConfig),
                "No configured audit queue was found");
        }

        AuditConfigReader.Result auditConfig;

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.MainPipeline.Register<AuditBehavior.Registration>();
            context.MainPipeline.Register<AttachCausationHeadersBehavior.Registration>();

            context.Container.ConfigureComponent<AuditQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Enabled, true)
                .ConfigureProperty(t => t.AuditQueue, auditConfig.Address);

            var behaviorConfig = context.Container.ConfigureComponent<AuditBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.AuditQueue, auditConfig.Address);

            behaviorConfig.ConfigureProperty(t => t.TimeToBeReceivedOnForwardedMessages, auditConfig.TimeToBeReceived);
        }

    }
}