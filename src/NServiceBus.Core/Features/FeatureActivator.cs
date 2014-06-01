namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

            foreach (var defaultSetting in feature.RegisteredDefaults)
            {
                defaultSetting(settings);
            }

            features.Add(feature);
        }

        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
        }

        public void SetupFeatures(FeatureConfigurationContext context)
        {
            var featuresToActivate = features.Where(f => IsEnabled(f.GetType()) && MeetsActivationCondition(f, context))
              .ToList();

            foreach (var feature in featuresToActivate)
            {
                ActivateFeature(feature, featuresToActivate, context);
            }
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

        static bool ActivateFeature(Feature feature, List<Feature> featuresToActivate, FeatureConfigurationContext context)
        {
            if (feature.IsActive)
            {
                return true;
            }

            Func<List<Type>, bool> dependencyActivator = dependenciesTypes =>
                                 {
                                     var dependantFeaturesToActivate = new List<Feature>();

                                     foreach (var dependency in dependenciesTypes.Select(dependencyType => featuresToActivate.SingleOrDefault(f => f.GetType() == dependencyType)).Where(dependency => dependency != null))
                                     {
                                         dependantFeaturesToActivate.Add(dependency);
                                     }

                                     return dependantFeaturesToActivate.Aggregate(false, (current, f) => current | ActivateFeature(f, dependantFeaturesToActivate, context));
                                 };

            if (feature.Dependencies.All(dependencyActivator))
            {
                feature.SetupFeature(context);
                return true;
            }

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

        bool MeetsActivationCondition(Feature feature, FeatureConfigurationContext context)
        {
            if (!feature.ShouldBeSetup(context))
            {
                return false;
            }

            return true;
        }

        readonly SettingsHolder settings;
        readonly List<Feature> features = new List<Feature>();

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