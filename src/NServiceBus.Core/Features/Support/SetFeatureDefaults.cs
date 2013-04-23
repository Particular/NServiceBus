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
                    if (((IConditionalFeature) feature).EnabledByDefault())
                    {
                        Feature.EnableByDefault(t);
                        Logger.DebugFormat("Feature {0} has requested to be enabled by default", t.Name);
                        
                    }
                    else
                    {
                        Feature.DisableByDefault(t);
                        Logger.DebugFormat("Feature {0} has requested to be disabled by default", t.Name);
                    }
                }
            });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}