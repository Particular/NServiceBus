namespace NServiceBus.Features
{
    using System;
    using Logging;

    public class EnableDefaultFeatures : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<Feature>(t =>
            {
                var feature = (Feature)Activator.CreateInstance(t);

                if (feature.IsEnabledByDefault)
                {
                    Feature.EnableByDefault(t);
                    Logger.DebugFormat("Feature {0} will be enabled by default", feature.Name);
                }
            });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}