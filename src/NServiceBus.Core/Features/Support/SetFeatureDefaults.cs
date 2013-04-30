namespace NServiceBus.Features
{
    using System;
    using Logging;

    public class SetFeatureDefaults : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<IFeature>(t =>
            {
                var feature = (IFeature)Activator.CreateInstance(t);

                if (feature is IConditionalFeature)
                {
                    Feature.EnableByDefault(t);
                    Logger.DebugFormat("Feature {0} is conditional and will be enabled by default", t.FeatureName());
                }
            });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}