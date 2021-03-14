namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Settings;

    class FeatureActivator
    {
        public FeatureActivator(SettingsHolder settings)
        {
            this.settings = settings;
        }

        internal List<FeatureDiagnosticData> Status => features.Select(f => f.Diagnostics).ToList();

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

        public async Task<FeatureDiagnosticData[]> SetupFeatures(FeatureConfigurationContext featureConfigurationContext, CancellationToken cancellationToken = default)
        {
            // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
            var sourceFeatures = Sort(features);

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
                await ActivateFeature(feature, enabledFeatures, featureConfigurationContext, cancellationToken).ConfigureAwait(false);
            }

            return features.Select(t => t.Diagnostics).ToArray();
        }

        public async Task StartFeatures(IServiceProvider builder, IMessageSession session, CancellationToken cancellationToken = default)
        {
            // sequential starting of startup tasks is intended, introducing concurrency here could break a lot of features.
            foreach (var feature in enabledFeatures.Where(f => f.Feature.IsActive))
            {
                foreach (var taskController in feature.TaskControllers)
                {
                    await taskController.Start(builder, session, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task StopFeatures(CancellationToken cancellationToken = default)
        {
            var featureStopTasks = enabledFeatures.Where(f => f.Feature.IsActive)
                .SelectMany(f => f.TaskControllers)
                .Select(task => task.Stop(cancellationToken));

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

        async Task<bool> ActivateFeature(FeatureInfo featureInfo, List<FeatureInfo> featuresToActivate, FeatureConfigurationContext featureConfigurationContext, CancellationToken cancellationToken)
        {
            if (featureInfo.Feature.IsActive)
            {
                return true;
            }

            Func<List<string>, CancellationToken, Task<bool>> dependencyActivator = async (dependencies, token) =>
            {
                var dependentFeaturesToActivate = new List<FeatureInfo>();

                foreach (var dependency in dependencies.Select(dependencyName => featuresToActivate
                    .SingleOrDefault(f => f.Feature.Name == dependencyName))
                    .Where(dependency => dependency != null))
                {
                    dependentFeaturesToActivate.Add(dependency);
                }

                var result = false;
                foreach (var f in dependentFeaturesToActivate)
                {
                    result |= await ActivateFeature(f, featuresToActivate, featureConfigurationContext, token).ConfigureAwait(false);
                }

                return result;
            };
            var featureType = featureInfo.Feature.GetType();

            var dependenciesAreMet = true;
            foreach (var item in featureInfo.Feature.Dependencies)
            {
                if (!await dependencyActivator(item, cancellationToken).ConfigureAwait(false))
                {
                    dependenciesAreMet = false;
                }
            }

            if (dependenciesAreMet)
            {
                featureInfo.Diagnostics.DependenciesAreMet = true;

                if (!HasAllPrerequisitesSatisfied(featureInfo.Feature, featureInfo.Diagnostics, featureConfigurationContext))
                {
                    settings.MarkFeatureAsDeactivated(featureType);
                    return false;
                }
                settings.MarkFeatureAsActive(featureType);

                await featureInfo.InitializeFrom(featureConfigurationContext, cancellationToken).ConfigureAwait(false);

                // because we reuse the context the task controller list needs to be cleared.
                featureConfigurationContext.TaskControllers.Clear();

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
        List<FeatureInfo> enabledFeatures = new List<FeatureInfo>();
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
            public IReadOnlyList<FeatureStartupTaskController> TaskControllers => taskControllers;

            public async Task InitializeFrom(FeatureConfigurationContext featureConfigurationContext, CancellationToken cancellationToken = default)
            {
                await Feature.SetupFeature(featureConfigurationContext, cancellationToken).ConfigureAwait(false);
                var featureStartupTasks = new List<string>();
                foreach (var controller in featureConfigurationContext.TaskControllers)
                {
                    taskControllers.Add(controller);
                    featureStartupTasks.Add(controller.Name);
                }
                Diagnostics.StartupTasks = featureStartupTasks;
                Diagnostics.Active = true;
            }

            public override string ToString()
            {
                return $"{Feature.Name} [{Feature.Version}]";
            }

            List<FeatureStartupTaskController> taskControllers = new List<FeatureStartupTaskController>();
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