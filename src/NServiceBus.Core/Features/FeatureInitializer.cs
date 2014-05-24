namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config;
    using Logging;

    class FeatureInitializer : IFinalizeConfiguration
    {
        public void FinalizeConfiguration(Configure config)
        {
            InitializeFeatures(config.Features, config);
            InitializeFeaturesControlledByCategories(config);
        }

        static void InitializeFeatures(IEnumerable<Feature> features, Configure config)
        {
            var statusText = new StringBuilder();


            foreach (var feature in features)
            {
                if (feature.Category != FeatureCategory.None)
                {
                    statusText.AppendLine(string.Format("{0} - Controlled by category {1}", feature,
                                                        feature.Category.Name));
                    continue;
                }

                if (!Feature.IsEnabled(feature.GetType()))
                {
                    statusText.AppendLine(string.Format("{0} - Disabled", feature));
                    continue;
                }

                feature.Initialize(config);

                statusText.AppendLine(string.Format("{0} - Enabled", feature));
            }

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        static void InitializeFeaturesControlledByCategories(Configure config)
        {
            var statusText = new StringBuilder();

            config.ForAllTypes<FeatureCategory>(t =>
            {
                if (t == typeof(FeatureCategory.NoneFeatureCategory))
                    return;

                var category = (FeatureCategory)Activator.CreateInstance(t);

                var featuresToInitialize = category.GetFeaturesToInitialize(config).ToList();

                statusText.AppendLine(string.Format("   - {0}", category.Name));

                foreach (var feature in config.Features.Where(f=>f.Category == category))
                {
                    var shouldBeInitialized = featuresToInitialize.Contains(feature);

                    if (shouldBeInitialized)
                    {
                        feature.Initialize(config);
                    }
                      

                    statusText.AppendLine(string.Format("       * {0} - {1}", feature.Name, shouldBeInitialized ? "Enabled" : "Disabled"));
                }

            });

            Logger.InfoFormat("Feature categories: \n{0}", statusText);
        }

        static ILog Logger = LogManager.GetLogger<FeatureInitializer>();
    }
}