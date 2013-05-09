namespace NServiceBus.Features
{
    using System;
    using System.Text;
    using Config;
    using Logging;

    public class FeatureInitializer : IFinalizeConfiguration, IWantToRunBeforeConfigurationIsFinalized
    {
        /// <summary>
        /// Go trough all conditional features and figure out if the should be enabled or not
        /// </summary>
        public void Run()
        {
            Configure.Instance.ForAllTypes<Feature>(t =>
                {
                    var feature = (Feature)Activator.CreateInstance(t);

                    if (feature.IsDefault && !Feature.IsEnabled(t))
                    {
                        Logger.InfoFormat("Default feature {0} has been explicitly disabled", feature.Name);
                        return;
                    }

                    if (feature.IsDefault && !feature.ShouldBeEnabled())
                    {
                        Feature.Disable(t);
                        Logger.DebugFormat("Default feature {0} disabled", feature.Name);
                    }
                });
        }

        public void FinalizeConfiguration()
        {
            var statusText = new StringBuilder();

            Configure.Instance.ForAllTypes<Feature>(t =>
                {
                    var feature = (Feature)Activator.CreateInstance(t);

                    if (!Feature.IsEnabled(t))
                    {
                        statusText.AppendLine(string.Format("{0} - Disabled", feature.Name));
                        return;
                    }

                    feature.Initialize();

                    statusText.AppendLine(string.Format("{0} - Enabled", feature.Name));
                });

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (FeatureInitializer));
    }
}