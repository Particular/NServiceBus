namespace NServiceBus.Features
{
    using NServiceBus.Transports;

    class ConsistencyGuaranteeFeature:Feature
    {
        public ConsistencyGuaranteeFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportDefault = context.Settings.Get<TransportDefinition>().GetDefaultConsistencyGuarantee();

            context.Pipeline.Register("ApplyDefaultConsistencyGuarantee", typeof(ApplyDefaultConsistencyGuaranteeBehavior), "Makes sure that the default guarantee is used if not explicitly requested by the caller");
            context.Container.ConfigureComponent(b => new ApplyDefaultConsistencyGuaranteeBehavior(transportDefault), DependencyLifecycle.SingleInstance);
        }
    }
}
