namespace NServiceBus.Core.Tests.Features
{
    using NServiceBus.Features;

    public class FakeFeatureConfigurationContext : FeatureConfigurationContext
    {
        public FakeFeatureConfigurationContext() : base(null, null, null, null, null, null)
        {
        }
    }
}
