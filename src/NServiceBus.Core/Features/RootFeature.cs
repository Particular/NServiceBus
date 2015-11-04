namespace NServiceBus.Features
{
    using System.Collections.Generic;

    /// <summary>
    /// A root feature that is always enabled.
    /// </summary>
    class RootFeature : Feature
    {
        public RootFeature()
        {
            EnableByDefault();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            return FeatureStartupTask.None;
        }
    }
}