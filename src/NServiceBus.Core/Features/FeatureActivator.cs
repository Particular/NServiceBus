namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    ///     Provides diagnostics data about <see cref="Feature" />s.
    /// </summary>
    public class FeaturesReport
    {
        internal FeaturesReport(IEnumerable<FeatureDiagnosticData> data)
        {
            this.data = data.ToList().AsReadOnly();
        }

        /// <summary>
        ///     List of <see cref="Feature" />s diagnostic data.
        /// </summary>
        public IList<FeatureDiagnosticData> Features
        {
            get { return data; }
        }

        readonly IList<FeatureDiagnosticData> data;
    }

    /// <summary>
    ///     <see cref="Feature" /> diagnostics data.
    /// </summary>
    public class FeatureDiagnosticData
    {
        /// <summary>
        ///     Gets the <see cref="Feature" /> name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Gets whether <see cref="Feature" /> is set to be enabled by default.
        /// </summary>
        public bool EnabledByDefault { get; internal set; }

        /// <summary>
        ///     Gets the status of the <see cref="Feature" />.
        /// </summary>
        public bool Active { get; internal set; }

        /// <summary>
        ///     Gets the status of the prerequisites for this <see cref="Feature" />.
        /// </summary>
        public PrerequisiteStatus PrerequisiteStatus { get; internal set; }

        /// <summary>
        ///     Gets the list of <see cref="Feature" />s that this <see cref="Feature" /> depends on.
        /// </summary>
        public IList<List<string>> Dependencies { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="Feature" /> version.
        /// </summary>
        public string Version { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="Feature" /> startup tasks.
        /// </summary>
        public IList<Type> StartupTasks { get; internal set; }

        /// <summary>
        ///     Gets whether all dependant <see cref="Feature" />s are activated.
        /// </summary>
        public bool DependenciesAreMeet { get; set; }
    }

    class FeatureActivator
    {
        public FeatureActivator(SettingsHolder settings)
        {
            this.settings = settings;
        }

        internal List<FeatureDiagnosticData> Status
        {
            get { return features.Select(f => f.Diagnostics).ToList(); }
        }

        public void Add(Feature feature)
        {
            if (feature.IsEnabledByDefault)
            {
                settings.EnableFeatureByDefault(feature.GetType());
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

        public FeaturesReport SetupFeatures(FeatureConfigurationContext context)
        {
            // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
            var sourceFeatures = Sort(features);

            var enabledFeatures = new List<FeatureState>();
            while (true)
            {
                var featureToActivate = sourceFeatures.FirstOrDefault(x => IsEnabled(x.Feature.GetType()));
                if (featureToActivate == null)
                {
                    break;
                }
                sourceFeatures.Remove(featureToActivate);
                enabledFeatures.Add(featureToActivate);
                foreach (var registeredDefault in featureToActivate.Feature.RegisteredDefaults)
                {
                    registeredDefault(settings);
                }
            }

            settings.PreventChanges();

            foreach (var feature in enabledFeatures)
            {
                ActivateFeature(feature, enabledFeatures, context);
            }

            return new FeaturesReport(features.Select(t => t.Diagnostics));
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
                    var task = (FeatureStartupTask) builder.Build(taskType);

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
                    var task = (FeatureStartupTask) builder.Build(taskType);

                    task.PerformStop();

                    DisposeIfNecessary(task);
                }
            }
        }

        static void DisposeIfNecessary(FeatureStartupTask task)
        {
            var disposableTask = task as IDisposable;
            if (disposableTask != null)
            {
                disposableTask.Dispose();
            }
        }

        static List<FeatureState> Sort(IEnumerable<FeatureState> features)
        {
            // Step 1: create nodes for graph
            var nameToNodeDict = new Dictionary<string, Node>();
            var allNodes = new List<Node>();
            foreach (var feature in features)
            {
                // create entries to preserve order within
                var node = new Node
                {
                    FeatureState = feature
                };

                nameToNodeDict[feature.Feature.Name] = node;
                allNodes.Add(node);
            }

            // Step 2: create edges dependencies
            foreach (var node in allNodes)
            {
                foreach (var dependencyName in node.FeatureState.Feature.Dependencies.SelectMany(listOfDependencyNames => listOfDependencyNames))
                {
                    Node referencedNode;
                    if (nameToNodeDict.TryGetValue(dependencyName, out referencedNode))
                    {
                        node.previous.Add(referencedNode);
                    }
                }
            }

            // Step 3: Perform Topological Sort
            var output = new List<FeatureState>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            return output;
        }

        static bool ActivateFeature(FeatureState featureState, List<FeatureState> featuresToActivate, FeatureConfigurationContext context)
        {
            if (featureState.Feature.IsActive)
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

            if (featureState.Feature.Dependencies.All(dependencyActivator))
            {
                featureState.Diagnostics.DependenciesAreMeet = true;

                if (!HasAllPrerequisitesSatisfied(featureState.Feature, featureState.Diagnostics, context))
                {
                    return false;
                }

                featureState.Feature.SetupFeature(context);
                featureState.Diagnostics.Active = true;

                return true;
            }

            featureState.Diagnostics.DependenciesAreMeet = false;

            return false;
        }

        bool IsEnabled(Type featureType)
        {
            return settings.GetOrDefault<bool>(featureType.FullName);
        }

        static bool HasAllPrerequisitesSatisfied(Feature feature, FeatureDiagnosticData diagnosticData, FeatureConfigurationContext context)
        {
            diagnosticData.PrerequisiteStatus = feature.CheckPrerequisites(context);

            return diagnosticData.PrerequisiteStatus.IsSatisfied;
        }

        readonly List<FeatureState> features = new List<FeatureState>();
        readonly SettingsHolder settings;

        class FeatureState
        {
            public FeatureState(Feature feature, FeatureDiagnosticData diagnostics)
            {
                Diagnostics = diagnostics;
                Feature = feature;
            }

            public FeatureDiagnosticData Diagnostics { get; private set; }
            public Feature Feature { get; private set; }
        }

        class Node
        {
            internal void Visit(ICollection<FeatureState> output)
            {
                if (visited)
                {
                    return;
                }
                visited = true;
                foreach (var n in previous)
                {
                    n.Visit(output);
                }
                output.Add(FeatureState);
            }

            internal FeatureState FeatureState;
            internal List<Node> previous = new List<Node>();
            bool visited;
        }
    }
}