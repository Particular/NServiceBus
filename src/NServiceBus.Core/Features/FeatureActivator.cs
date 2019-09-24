namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Settings;

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

            features.Add(new FeatureInfo(feature, new FeatureDiagnosticData
            {
                EnabledByDefault = feature.IsEnabledByDefault,
                Name = feature.Name,
                Version = feature.Version,
                Dependencies = feature.Dependencies.AsReadOnly()
            }));
        }

        public FeatureDiagnosticData[] SetupFeatures(FeatureConfigurationContext featureConfigurationContext)
        {
            // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
            var sourceFeatures = Sort(features);

            var enabledFeatures = new List<FeatureInfo>();
            while (true)
            {
                var featureToActivate = sourceFeatures.FirstOrDefault(x => settings.IsFeatureEnabled(x.Feature.GetType()));
                if (featureToActivate == null)
                {
                    break;
                }
                sourceFeatures.Remove(featureToActivate);
                enabledFeatures.Add(featureToActivate);
                featureToActivate.Feature.ConfigureDefaults(settings);
            }

            foreach (var feature in enabledFeatures)
            {
                ActivateFeature(feature, enabledFeatures, featureConfigurationContext);
            }

            settings.PreventChanges();

            return features.Select(t => t.Diagnostics).ToArray();
        }

        public async Task StartFeatures(IBuilder builder, IMessageSession session)
        {
            foreach (var feature in features.Where(f => f.Feature.IsActive))
            {
                foreach (var taskController in feature.TaskControllers)
                {
                    await taskController.Start(builder, session).ConfigureAwait(false);
                }
            }
        }

        public Task StopFeatures(IMessageSession session)
        {
            var featureStopTasks = features.Where(f => f.Feature.IsActive)
                .SelectMany(f => f.TaskControllers)
                .Select(task => task.Stop(session));

            return Task.WhenAll(featureStopTasks);
        }

        static List<FeatureInfo> Sort(IEnumerable<FeatureInfo> features)
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
                    if (nameToNodeDict.TryGetValue(dependencyName, out var referencedNode))
                    {
                        node.previous.Add(referencedNode);
                    }
                }
            }

            // Step 3: Perform Topological Sort
            var output = new List<FeatureInfo>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            // Step 4: DFS to check if we have an directed acyclic graph
            foreach (var node in allNodes)
            {
                if (DirectedCycleExistsFrom(node, new Node[]
                {
                }))
                {
                    throw new ArgumentException("Cycle in dependency graph detected");
                }
            }

            return output;
        }

        static bool DirectedCycleExistsFrom(Node node, Node[] visitedNodes)
        {
            if (node.previous.Any())
            {
                if (visitedNodes.Any(n => n == node))
                {
                    return true;
                }

                var newVisitedNodes = visitedNodes.Union(new[]
                {
                    node
                }).ToArray();

                foreach (var subNode in node.previous)
                {
                    if (DirectedCycleExistsFrom(subNode, newVisitedNodes))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool ActivateFeature(FeatureInfo featureInfo, List<FeatureInfo> featuresToActivate, FeatureConfigurationContext featureConfigurationContext)
        {
            if (featureInfo.Feature.IsActive)
            {
                return true;
            }

            Func<List<string>, bool> dependencyActivator = dependencies =>
            {
                var dependentFeaturesToActivate = new List<FeatureInfo>();

                foreach (var dependency in dependencies.Select(dependencyName => featuresToActivate
                    .SingleOrDefault(f => f.Feature.Name == dependencyName))
                    .Where(dependency => dependency != null))
                {
                    dependentFeaturesToActivate.Add(dependency);
                }
                return dependentFeaturesToActivate.Aggregate(false, (current, f) => current | ActivateFeature(f, featuresToActivate, featureConfigurationContext));
            };
            var featureType = featureInfo.Feature.GetType();
            if (featureInfo.Feature.Dependencies.All(dependencyActivator))
            {
                featureInfo.Diagnostics.DependenciesAreMet = true;

                if (!HasAllPrerequisitesSatisfied(featureInfo.Feature, featureInfo.Diagnostics, featureConfigurationContext))
                {
                    settings.MarkFeatureAsDeactivated(featureType);
                    return false;
                }
                settings.MarkFeatureAsActive(featureType);
                featureInfo.Feature.SetupFeature(featureConfigurationContext);

                featureInfo.TaskControllers = new List<FeatureStartupTaskController>(featureConfigurationContext.TaskControllers);
                featureConfigurationContext.TaskControllers.Clear();

                featureInfo.Diagnostics.StartupTasks = featureInfo.TaskControllers.Select(d => d.Name).ToList();
                featureInfo.Diagnostics.Active = true;
                return true;
            }
            settings.MarkFeatureAsDeactivated(featureType);
            featureInfo.Diagnostics.DependenciesAreMet = false;
            return false;
        }

        static bool HasAllPrerequisitesSatisfied(Feature feature, FeatureDiagnosticData diagnosticData, FeatureConfigurationContext context)
        {
            diagnosticData.PrerequisiteStatus = feature.CheckPrerequisites(context);

            return diagnosticData.PrerequisiteStatus.IsSatisfied;
        }

        List<FeatureInfo> features = new List<FeatureInfo>();
        SettingsHolder settings;

        class FeatureInfo
        {
            public FeatureInfo(Feature feature, FeatureDiagnosticData diagnostics)
            {
                Diagnostics = diagnostics;
                Feature = feature;
            }

            public FeatureDiagnosticData Diagnostics { get; }
            public Feature Feature { get; }
            public IReadOnlyList<FeatureStartupTaskController> TaskControllers { get; set; }

            public override string ToString()
            {
                return $"{Feature.Name} [{Feature.Version}]";
            }
        }

        class Node
        {
            internal void Visit(ICollection<FeatureInfo> output)
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

            internal FeatureInfo FeatureState;
            internal List<Node> previous = new List<Node>();
            bool visited;
        }
    }
}