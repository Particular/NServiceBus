namespace NServiceBus.Features
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// Settings for the various features
    /// </summary>
    public class FeatureSettings:IEnumerable<Feature>
    {
        readonly Configure config;

        public FeatureSettings(Configure config)
        {
            this.config = config;
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        public FeatureSettings Enable<T>() where T : Feature
        {
            Feature.Enable<T>();

            return this;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public FeatureSettings Disable<T>() where T : Feature
        {
            Feature.Disable<T>();

            return this;
        }

        public void Add(Feature feature)
        {
            if (feature.IsEnabledByDefault)
            {
                Feature.EnableByDefault(feature.GetType());    
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

        public void EnableByDefault<T>() where T:Feature
        {
            Feature.EnableByDefault<T>();    
        }

        public bool IsEnabled<T>() where T:Feature
        {
            return Feature.IsEnabled<T>();
        }

        public void DisableFeaturesAsNeeded()
        {
            var features = config.Features;

            DisableFeaturesThatAskedToBeDisabled(config);

            DisableFeaturesThatAreDependingOnDisabledFeatures(features);
        }

        static void DisableFeaturesThatAskedToBeDisabled(Configure config)
        {
            foreach (var feature in config.Features)
            {
                if (!Feature.IsEnabled(feature.GetType()))
                {
                    if (feature.IsEnabledByDefault)
                    {
                        Logger.InfoFormat("Default feature {0} has been explicitly disabled", feature.Name);
                    }

                    continue;
                }

                if (!feature.ShouldBeEnabled(config))
                {
                    Feature.Disable(feature.GetType());
                    Logger.DebugFormat("Default feature {0} has requested to be disabled", feature.Name);
                }
            }
        }

        static void DisableFeaturesThatAreDependingOnDisabledFeatures(IEnumerable<Feature> features)
        {
            features
                 .Where(f => f.Dependencies.Any(dependency => !Feature.IsEnabled(dependency)))
                 .ToList()
                 .ForEach(toBeDisabled =>
                 {
                     Feature.Disable(toBeDisabled.GetType());
                     Logger.InfoFormat("Feature {0} has been disabled since its depending on the following disabled features: {1}", toBeDisabled.Name, string.Join(",", toBeDisabled.Dependencies.Where(d => !Feature.IsEnabled(d))));
                 });
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureSettings));
    }
}