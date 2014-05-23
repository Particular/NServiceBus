namespace NServiceBus.Features
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Logging;
    using Settings;

    /// <summary>
    /// Settings for the various features
    /// </summary>
    public class FeatureSettings : IEnumerable<Feature>
    {
        readonly Configure config;
        readonly SettingsHolder settings;

        public FeatureSettings(Configure config)
        {
            this.config = config;
            settings = config.Settings;
        }

        public FeatureSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        public Configure Enable<T>() where T : Feature
        {
            return Enable(typeof(T));
        }

        public Configure Enable(Type featureType)
        {
            settings.Set(featureType.FullName, true);

            return config;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public Configure Disable<T>() where T : Feature
        {
            return Disable(typeof(T));
        }

        public Configure Disable(Type featureType)
        {
            settings.Set(featureType.FullName, false);

            return config;
        }

        public void Add(Feature feature)
        {
            if (feature.IsEnabledByDefault)
            {
                EnableByDefault(feature.GetType());
            }

            features.Add(feature);
        }

        List<Feature> features = new List<Feature>();
        public IEnumerator<Feature> GetEnumerator()
        {
            return features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void EnableByDefault<T>() where T : Feature
        {
            EnableByDefault(typeof(T));
        }

        public void EnableByDefault(Type featureType)
        {
            settings.SetDefault(featureType.FullName, true);
        }

        bool IsEnabled<T>() where T : Feature
        {
            return IsEnabled(typeof(T));
        }

        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
        }

        public void SetupFeatures()
        {
            var statusText = new StringBuilder();

            var context = new FeatureConfigurationContext(settings, config.Configurer, config.Pipeline, config.TypesToScan);

            var featuresToActivate = features.Where(f => IsEnabled(f.GetType()) && MeetsActivationCondition(f, statusText,context))
              .ToList();

            foreach (var feature in featuresToActivate)
            {
                feature.SetupFeature(context);
             

                statusText.AppendLine(string.Format("{0} - Activated", feature));
            }

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        bool MeetsActivationCondition(Feature feature, StringBuilder statusText,FeatureConfigurationContext context)
        {
            if (!feature.ShouldBeSetup(context))
            {

                statusText.AppendLine(string.Format("{0} - Activation condition(s) not fullfilled", feature));
                return false;
            }

            return true;
        }

        //void DisableFeaturesThatAreDependingOnDisabledFeatures()
        //{
        //    features.Where(f => f.Dependencies.Any(dependency => !IsEnabled(dependency)))
        //         .ToList()
        //         .ForEach(toBeDisabled =>
        //         {
        //             Disable(toBeDisabled.GetType());
        //             Logger.InfoFormat("Feature {0} has been disabled since its depending on the following disabled features: {1}", toBeDisabled.Name, string.Join(",", toBeDisabled.Dependencies.Where(d => !IsEnabled(d))));
        //         });
        //}

        static ILog Logger = LogManager.GetLogger<FeatureSettings>();

        public bool IsActivated<T>() where T:Feature
        {
            return features.Single(f => f.GetType() == typeof(T)).IsActivated;
        }
    }
}