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
            InitializeFeatures(config.Features);
            InitializeFeaturesControlledByCategories();
        }

        static void InitializeFeatures(IEnumerable<Feature> features)
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

                feature.Initialize(Configure.Instance);

                statusText.AppendLine(string.Format("{0} - Enabled", feature));
            }

            Logger.InfoFormat("Features: \n{0}", statusText);
        }

        static void InitializeFeaturesControlledByCategories()
        {
            var statusText = new StringBuilder();

            Configure.Instance.ForAllTypes<FeatureCategory>(t =>
            {
                if (t == typeof(FeatureCategory.NoneFeatureCategory))
                    return;

                var category = (FeatureCategory)Activator.CreateInstance(t);

                var featuresToInitialize = category.GetFeaturesToInitialize().ToList();

                statusText.AppendLine(string.Format("   - {0}", category.Name));

                foreach (var feature in category.GetAllAvailableFeatures())
                {
                    var shouldBeInitialized = featuresToInitialize.Contains(feature);

                    if (shouldBeInitialized)
                        feature.Initialize(Configure.Instance);

                    statusText.AppendLine(string.Format("       * {0} - {1}", feature.Name, shouldBeInitialized ? "Enabled" : "Disabled"));
                }

            });

            Logger.InfoFormat("Feature categories: \n{0}", statusText);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FeatureInitializer));
    }
}