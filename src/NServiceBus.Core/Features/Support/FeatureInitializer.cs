namespace NServiceBus.Features
{
    using System;
    using System.Text;
    using Config;
    using Logging;

    public class FeatureInitializer : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            var statusText = new StringBuilder();

            Configure.Instance.ForAllTypes<IFeature>(t =>
                {
                    if (!Feature.IsEnabled(t))
                    {
                        statusText.AppendLine(string.Format("{0} - Disabled", t.Name));
                        return;
                    }

                    var feature = (IFeature)Activator.CreateInstance(t);
                 
                    if (feature is IConditionalFeature && !((IConditionalFeature)feature).ShouldBeEnabled())
                    {
                        statusText.AppendLine(string.Format("{0} - Conditionally disabled", t.Name));
                        return;
                    }

                    feature.Initialize();

                    statusText.AppendLine(string.Format("{0} - Enabled", t.Name));
                });

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}