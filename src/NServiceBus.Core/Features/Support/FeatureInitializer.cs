namespace NServiceBus.Features
{
    using System;
    using Config;
    using Logging;

    public class FeatureInitializer : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            Configure.Instance.ForAllTypes<IFeature>(t =>
                {
                    if (!Feature.IsEnabled(t))
                        return;

                    var feature = (IFeature)Activator.CreateInstance(t);
                    feature.Initalize();

                    Logger.DebugFormat("Feature initalized: {0}",t.FullName);
                });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}