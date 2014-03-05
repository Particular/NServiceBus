namespace NServiceBus.Features
{
    using System.Text;
    using Config;
    using Logging;

    /// <summary>
    /// Displays the current status of the features
    /// </summary>
    public class FeatureStatus:IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var statusText = new StringBuilder();

            Configure.Instance.ForAllTypes<IFeature>(t => statusText.AppendLine(string.Format("{0} - {1}", t.Name,Feature.IsEnabled(t) ? "Enabled" : "Disabled")));

            Logger.InfoFormat("Features: \n{0}",statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureStatus));
    }
}