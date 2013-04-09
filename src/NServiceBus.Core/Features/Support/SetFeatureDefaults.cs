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

                if (feature is IConditionalFeature && ((IConditionalFeature)feature).EnabledByDefault())
                {
                    Feature.Enable(t);
                    Logger.DebugFormat("Feature {0} has requested to be enabled by default",t.Name);
                    return;
                }

                Feature.Disable(t);
                Logger.DebugFormat("Feature {0} is disabled by default",t.Name);
    
            });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}