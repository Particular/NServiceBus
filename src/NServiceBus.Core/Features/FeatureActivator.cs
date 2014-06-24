namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config;
    using Logging;
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
        /// Gets whether all prerequisites are fulfilled for this <see cref="Feature"/>.
        /// </summary>
        public bool PrerequisitesFulfilled { get; internal set; }
        
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

    class DisplayDiagnosticsForFeatures : IWantToRunWhenConfigurationIsComplete
    {
        public FeaturesReport FeaturesReport { get; set; }
        static ILog Logger = LogManager.GetLogger<DisplayDiagnosticsForFeatures>();
        public void Run(Configure config)
        {
            var statusText = new StringBuilder();

            statusText.AppendLine("------------- FEATURES ----------------");

            foreach (var diagnosticData in FeaturesReport.Features)
            {
                statusText.AppendLine(string.Format("Name: {0}", diagnosticData.Name));
                statusText.AppendLine(string.Format("Version: {0}", diagnosticData.Version));
                statusText.AppendLine(string.Format("Enabled by Default: {0}", diagnosticData.EnabledByDefault ? "Yes" : "No"));
                statusText.AppendLine(string.Format("Status: {0}", diagnosticData.Active ? "Enabled" : "Disabled"));
                if (!diagnosticData.Active)
                {
                    statusText.Append("Deactivation reason: ");
                    if (!diagnosticData.PrerequisitesFulfilled)
                    {
                        statusText.AppendLine(string.Format("Did not fulfill one of the Prerequisites"));
                    } 
                    else if (!diagnosticData.DependenciesAreMeet)
                    {
                        statusText.AppendLine(string.Format("Did not meet one of the dependencies: {0}", String.Join(",", diagnosticData.Dependencies.Select(t => "[" + String.Join(",", t.Select(t1 => t1)) + "]"))));
                    }
                }
                else
                {
                    statusText.AppendLine(string.Format("Dependencies: {0}", diagnosticData.Dependencies.Count == 0 ? "None" : String.Join(",", diagnosticData.Dependencies.Select(t => "[" + String.Join(",", t.Select(t1 => t1)) + "]"))));
                    statusText.AppendLine(string.Format("Startup Tasks: {0}", diagnosticData.StartupTasks.Count == 0 ? "None" : String.Join(",", diagnosticData.StartupTasks.Select(t => t.Name))));
                }

                statusText.AppendLine();
            }

            Logger.Info(statusText.ToString());
        }
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

            features.Add(Tuple.Create(feature, new FeatureDiagnosticData
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
            var featuresToActivate = features.Where(f => IsEnabled(f.Item1.GetType()) && (f.Item2.PrerequisitesFulfilled = MeetsActivationCondition(f.Item1, context)))
                .ToList();

            foreach (var feature in featuresToActivate)
            {
                ActivateFeature(feature, featuresToActivate, context);
            }

            context.Container.RegisterSingleton<FeaturesReport>(new FeaturesReport(features.Select(t=>t.Item2)));
        }

        public void RegisterStartupTasks(IConfigureComponents container)
        {
            foreach (var feature in features.Where(f => f.Item1.IsActive))
            {
                foreach (var taskType in feature.Item1.StartupTasks)
                {
                    container.ConfigureComponent(taskType, DependencyLifecycle.SingleInstance);
                }
            }
        }

        public void StartFeatures(IBuilder builder)
        {
            foreach (var feature in features.Where(f => f.Item1.IsActive))
            {
                foreach (var taskType in feature.Item1.StartupTasks)
                {
                    var task = (FeatureStartupTask)builder.Build(taskType);

                    task.PerformStartup();
                }
            }
        }

        static bool ActivateFeature(Tuple<Feature, FeatureDiagnosticData> feature, List<Tuple<Feature, FeatureDiagnosticData>> featuresToActivate, FeatureConfigurationContext context)
        {
            if (feature.Item1.IsActive)
            {
                return true;
            }

            Func<List<string>, bool> dependencyActivator = dependencies =>
                                 {
                                     var dependantFeaturesToActivate = new List<Tuple<Feature, FeatureDiagnosticData>>();

                                     foreach (var dependency in dependencies.Select(dependencyName => featuresToActivate
                                         .SingleOrDefault(f => f.Item1.Name == dependencyName))
                                         .Where(dependency => dependency != null))
                                     {
                                         dependantFeaturesToActivate.Add(dependency);
                                     }
                                     var hasAllUpstreamDepsBeenActivated = dependantFeaturesToActivate.Aggregate(false, (current, f) => current | ActivateFeature(f, featuresToActivate, context));

                                     return hasAllUpstreamDepsBeenActivated;
                                 };

            if (feature.Item1.Dependencies.All(dependencyActivator))
            {
                feature.Item1.SetupFeature(context);
                feature.Item2.Active = true;
                feature.Item2.DependenciesAreMeet = true;

                return true;
            }

            feature.Item2.DependenciesAreMeet = false;

            return false;
        }

        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
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
        readonly List<Tuple<Feature, FeatureDiagnosticData>> features = new List<Tuple<Feature, FeatureDiagnosticData>>();

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