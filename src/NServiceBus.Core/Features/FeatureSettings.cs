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
        readonly SettingsHolder settings;
        readonly Configure config;

        public FeatureSettings(Configure config)
        {
            settings = config.Settings;
            this.config = config;
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

            var context = new FeatureConfigurationContext(config);
        

            var featuresToActivate = features.Where(f => IsEnabled(f.GetType()) && MeetsActivationCondition(f, statusText, context))
              .ToList();

            foreach (var feature in featuresToActivate)
            {
                ActivateFeature(feature, statusText, featuresToActivate, context);
            }

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        bool ActivateFeature(Feature feature, StringBuilder statusText, List<Feature> featuresToActivate, FeatureConfigurationContext context)
        {
            if (feature.IsActive)
            {
                return true;
            }

            if (feature.Dependencies.All(dependencyType =>
            {
                var dependency = featuresToActivate.SingleOrDefault(f => f.GetType() == dependencyType);


                if (dependency == null)
                {
                    return false;
                }

                return ActivateFeature(dependency, statusText, featuresToActivate,context);
            }))
            {
                feature.SetupFeature(context);


                statusText.AppendLine(string.Format("{0} - Activated", feature));

                return true;
            }
            statusText.AppendLine(string.Format("{0} - Not activated due to dependencies not being available: {1}", feature, string.Join(";", feature.Dependencies.Select(t => t.Name))));
            return false;
        }

        bool MeetsActivationCondition(Feature feature, StringBuilder statusText, FeatureConfigurationContext context)
        {
            if (!feature.ShouldBeSetup(context))
            {

                statusText.AppendLine(string.Format("{0} - setup prerequisites(s) not fullfilled", feature));
                return false;
            }

            return true;
        }

        
        static ILog Logger = LogManager.GetLogger<FeatureSettings>();

        public bool IsActivated<T>() where T : Feature
        {
            var feature = features.SingleOrDefault(f => f.GetType() == typeof(T));

            return feature != null && feature.IsActive;
        }
    }
}