namespace NServiceBus.Core.Tests.Features
{
    using NServiceBus.Features;

    static class FeatureTestExtensions
    {
        public static void SetupFeaturesForTest(this FeatureActivator activator)
        {
            activator.SetupFeatures(null, null, null, null);
        }
    }
}