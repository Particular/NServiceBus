namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline;
    using Transports;

    public class Outbox : Feature
    {
        public Outbox()
        {
            Defaults(s => s.SetDefault(TimeToKeepDeduplicationEntries, TimeSpan.FromDays(30)));
        }


        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<OutboxDeduplicationBehavior.OutboxDeduplicationRegistration>();
            context.Pipeline.Register<OutboxRecordBehavior.OutboxRecorderRegistration>();
            context.Pipeline.Replace(WellKnownBehavior.DispatchMessageToTransport, typeof(OutboxSendBehavior), "Sending behavior with a delay sending until all business transactions are committed to the outbox storage");

            //make the audit use the outbox as well
            if (context.Container.HasComponent<IAuditMessages>())
            {
                context.Container.ConfigureComponent<OutboxAwareAuditer>(DependencyLifecycle.InstancePerCall);
            }
        }

        public const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";

    }
}