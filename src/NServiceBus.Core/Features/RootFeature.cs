namespace NServiceBus.Features
{
    /// <summary>
    /// A root feature that is always enabled.
    /// </summary>
    class RootFeature : Feature
    {
        public RootFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}