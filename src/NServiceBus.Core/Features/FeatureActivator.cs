namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    /// Provides diagnostics data about <see cref="Feature"/>s.
    /// </summary>
    public class FeaturesReport
    {
        readonly IList<FeatureDiagnosticData> data;

        internal FeaturesReport(IEnumerable<FeatureDiagnosticData> data)
        {
            this.data = data.ToList().AsReadOnly();
        }

        /// <summary>
        /// List of <see cref="Feature"/>s diagnostic data.
        /// </summary>
        public IList<FeatureDiagnosticData> Features
        {
            get { return data; }
        }
    }

    /// <summary>
    /// <see cref="Feature"/> diagnostics data.
    /// </summary>
    public class FeatureDiagnosticData
    {
        /// <summary>
        /// Gets the <see cref="Feature"/> name.
        /// </summary>
        public string Name { get; internal set; }
        
        /// <summary>
        /// Gets whether <see cref="Feature"/> is set to be enabled by default.
        /// </summary>
        public bool EnabledByDefault { get; internal set; }
        
        /// <summary>
        /// Gets the status of the <see cref="Feature"/>.
        /// </summary>
        public bool Active { get; internal set; }
        
        /// <summary>
        /// Gets the status of the prerequisites for this <see cref="Feature"/>.
        /// </summary>
        public PrerequisiteStatus PrerequisiteStatus { get; internal set; }
        
        /// <summary>
        /// Gets the list of <see cref="Feature"/>s that this <see cref="Feature"/> depends on.
        /// </summary>
        public IList<List<string>> Dependencies { get; internal set; }
        
        /// <summary>
        /// Gets the <see cref="Feature"/> version.
        /// </summary>
        public string Version { get; internal set; }
        
        /// <summary>
        /// Gets the <see cref="Feature"/> startup tasks.
        /// </summary>
        public IList<Type> StartupTasks { get; internal set; }
        
        /// <summary>
        /// Gets whether all dependant <see cref="Feature"/>s are activated.
        /// </summary>
        public bool DependenciesAreMeet { get; set; }
    }

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

            features.Add(new FeatureState(feature, new FeatureDiagnosticData
            {
                EnabledByDefault = feature.IsEnabledByDefault,
                Name = feature.Name,
                Version = feature.Version,
                StartupTasks = feature.StartupTasks.AsReadOnly(),
                Dependencies = feature.Dependencies.AsReadOnly(),
            }));
        }

        public void SetupFeatures(FeatureConfigurationContext context)
        {
            var featuresToActivate = features.Where(featureState => IsEnabled(featureState.Feature.GetType()) && 
                MeetsActivationCondition(featureState.Feature,featureState.Diagnostics, context))
                .ToList();

            foreach (var feature in featuresToActivate)
            {
                ActivateFeature(feature, featuresToActivate, context);
            }

            context.Container.RegisterSingleton<FeaturesReport>(new FeaturesReport(features.Select(t=>t.Diagnostics)));
        }

        public void RegisterStartupTasks(IConfigureComponents container)
        {
            foreach (var feature in features.Where(f => f.Feature.IsActive))
            {
                foreach (var taskType in feature.Feature.StartupTasks)
                {
                    container.ConfigureComponent(taskType, DependencyLifecycle.SingleInstance);
                }
            }
        }

        public void StartFeatures(IBuilder builder)
        {
            foreach (var feature in features.Where(f => f.Feature.IsActive))
            {
                foreach (var taskType in feature.Feature.StartupTasks)
                {
                    var task = (FeatureStartupTask)builder.Build(taskType);

                    task.PerformStartup();
                }
            }
        }

        public void StopFeatures(IBuilder builder)
        {
            foreach (var feature in features.Where(f => f.Feature.IsActive))
            {
                foreach (var taskType in feature.Feature.StartupTasks)
                {
                    var task = (FeatureStartupTask)builder.Build(taskType);

                    task.PerformStop();
                }
            }
        }

        static bool ActivateFeature(FeatureState feature, List<FeatureState> featuresToActivate, FeatureConfigurationContext context)
        {
            if (feature.Feature.IsActive)
            {
                return true;
            }

            Func<List<string>, bool> dependencyActivator = dependencies =>
                                 {
                                     var dependantFeaturesToActivate = new List<FeatureState>();

                                     foreach (var dependency in dependencies.Select(dependencyName => featuresToActivate
                                         .SingleOrDefault(f => f.Feature.Name == dependencyName))
                                         .Where(dependency => dependency != null))
                                     {
                                         dependantFeaturesToActivate.Add(dependency);
                                     }
                                     var hasAllUpstreamDepsBeenActivated = dependantFeaturesToActivate.Aggregate(false, (current, f) => current | ActivateFeature(f, featuresToActivate, context));

                                     return hasAllUpstreamDepsBeenActivated;
                                 };

            if (feature.Feature.Dependencies.All(dependencyActivator))
            {
                feature.Feature.SetupFeature(context);
                feature.Diagnostics.Active = true;
                feature.Diagnostics.DependenciesAreMeet = true;

                return true;
            }

            feature.Diagnostics.DependenciesAreMeet = false;

            return false;
        }

        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
        }

        bool MeetsActivationCondition(Feature feature,FeatureDiagnosticData diagnosticData, FeatureConfigurationContext context)
        {
            diagnosticData.PrerequisiteStatus = feature.CheckPrerequisites(context);

            return diagnosticData.PrerequisiteStatus.IsSatisfied;
        }

        internal List<FeatureDiagnosticData> Status
        {
            get
            {
                return features.Select(f=>f.Diagnostics).ToList();
            }
        }

        readonly SettingsHolder settings;

        readonly List<FeatureState> features = new List<FeatureState>();

        class FeatureState
        {
            public FeatureState(Feature feature,FeatureDiagnosticData diagnostics)
            {
                Diagnostics = diagnostics;
                Feature = feature;
            }

            public FeatureDiagnosticData Diagnostics { get; private set; }
            public Feature Feature { get; private set; }
        }

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
                FeatureActivator.StopFeatures(Builder);
            }
        }
    }
}