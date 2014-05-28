namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Logging;
    using ObjectBuilder;
    using Settings;

    class FeatureActivator
    {
        public FeatureActivator(SettingsHolder settings)
        {
            this.settings = settings;
        }


        public void Add(Feature feature)
        {
            if (feature.IsEnabledByDefault)
            {
                settings.EnableFeatureByDefault(feature.GetType());
            }

            features.Add(feature);
        }


        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
        }

        public void SetupFeatures(FeatureConfigurationContext context)
        {
            var statusText = new StringBuilder();

            var featuresToActivate = features.Where(f => IsEnabled(f.GetType()) && MeetsActivationCondition(f, statusText, context))
              .ToList();

            foreach (var feature in featuresToActivate)
            {
                ActivateFeature(feature, statusText, featuresToActivate, context);
            }

            Logger.InfoFormat("Features: \n{0}", statusText);
        }



        public void RegisterStartupTasks(IConfigureComponents container)
        {
            foreach (var feature in features.Where(f => f.IsActive))
            {
                foreach (var taskType in feature.StartupTasks)
                {
                    container.ConfigureComponent(taskType, DependencyLifecycle.SingleInstance);
                }
            }
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

                return ActivateFeature(dependency, statusText, featuresToActivate, context);
            }))
            {
                feature.SetupFeature(context);


                statusText.AppendLine(string.Format("{0} - Activated", feature));

                return true;
            }
            statusText.AppendLine(string.Format("{0} - Not activated due to dependencies not being available: {1}", feature, string.Join(";", feature.Dependencies.Select(t => t.Name))));
            return false;
        }


        public void StartFeatures(IBuilder builder)
        {
            foreach (var feature in features.Where(f => f.IsActive))
            {
                foreach (var taskType in feature.StartupTasks)
                {
                    var task = (FeatureStartupTask)builder.Build(taskType);

                    task.PerformStartup();
                }
            }
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

        readonly SettingsHolder settings;
        readonly List<Feature> features = new List<Feature>();

        static ILog Logger = LogManager.GetLogger<FeatureActivator>();

        class Runner : IWantToRunWhenBusStartsAndStops
        {

            public IBuilder Builder { get; set; }

            public FeatureActivator FeatureActivator { get; set; }

            
            public void Start()
            {
                FeatureActivator.StartFeatures(Builder);
            }

            public void Stop()
            {
            }
        }

    }
}