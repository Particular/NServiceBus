#nullable enable

namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Settings;

class FeatureRegistry(SettingsHolder settings, FeatureFactory factory)
{
    internal List<FeatureDiagnosticData> Status => [.. added.Values.Select(f => f.Diagnostics)];

    public void Add(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.ContainsKey(featureName))
        {
            Add(factory.CreateFeature(featureType));
        }
    }

    public void Add(Feature feature)
    {
        if (added.ContainsKey(feature.Name))
        {
            return;
        }

        if (feature.IsEnabledByDefault)
        {
            _ = settings.EnableFeatureByDefault(feature.GetType());
        }

        foreach (var dependency in feature.Dependencies.SelectMany(d => d))
        {
            var featureName = dependency.FeatureType != null ? Feature.GetFeatureName(dependency.FeatureType) : dependency.FeatureName;
            if (!added.ContainsKey(featureName))
            {
                var dependentFeatureType = dependency.FeatureType ?? Type.GetType(dependency.FeatureName, false);
                if (dependentFeatureType != null)
                {
                    var dependentFeature = factory.CreateFeature(dependentFeatureType);
                    if (dependency.EnabledByDefault)
                    {
                        feature.Defaults(s => s.EnableFeatureByDefault(dependentFeatureType));
                    }

                    Add(dependentFeature);
                }
            }
            else
            {
                if (dependency.EnabledByDefault)
                {
                    // TODO Move to internal extension?
                    // Also we seem to always assume Feature.Name == FullName
                    feature.Defaults(s => settings.SetDefault(dependency.FeatureName, FeatureState.Enabled));
                }
            }
        }

        added.Add(feature.Name, new FeatureInfo(feature, new FeatureDiagnosticData
        {
            EnabledByDefault = feature.IsEnabledByDefault,
            Name = feature.Name,
            Version = feature.Version,
            Dependencies = feature.Dependencies.Select(d => d.Where(x => !x.EnabledByDefault).Select(x => x.FeatureName).ToList().AsReadOnly())
                .Where(innerList => innerList.Count > 0).ToList().AsReadOnly(),
            PrerequisiteStatus = new PrerequisiteStatus(),
            StartupTasks = []
        }));
    }

    public FeatureDiagnosticData[] SetupFeatures(FeatureConfigurationContext featureConfigurationContext)
    {
        // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
        var sourceFeatures = Sort(added.Values);

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

        return [.. added.Values.Select(t => t.Diagnostics)];
    }

    public async Task StartFeatures(IServiceProvider builder, IMessageSession session, CancellationToken cancellationToken = default)
    {
        var startedTaskControllers = new List<FeatureStartupTaskController>();

        // sequential starting of startup tasks is intended, introducing concurrency here could break a lot of features.
        foreach (var feature in enabledFeatures.Where(f => f.Feature.IsActive))
        {
            foreach (var taskController in feature.TaskControllers)
            {
                try
                {
                    await taskController.Start(builder, session, cancellationToken).ConfigureAwait(false);
                }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - OCE handling is the same
                catch (Exception)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
                {
                    await Task.WhenAll(startedTaskControllers.Select(controller => controller.Stop(session, cancellationToken))).ConfigureAwait(false);

                    throw;
                }

                startedTaskControllers.Add(taskController);
            }
        }
    }

    public Task StopFeatures(IMessageSession session, CancellationToken cancellationToken = default)
    {
        var featureStopTasks = enabledFeatures.Where(f => f.Feature.IsActive)
            .SelectMany(f => f.TaskControllers)
            .Select(task => task.Stop(session, cancellationToken));

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
            foreach (var dependencyName in node.FeatureState.Diagnostics.Dependencies.SelectMany(listOfDependencyNames => listOfDependencyNames))
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
            if (DirectedCycleExistsFrom(node, []))
            {
                throw new ArgumentException("Cycle in dependency graph detected");
            }
        }

        return output;
    }

    static bool DirectedCycleExistsFrom(Node node, Node[] visitedNodes)
    {
        if (node.previous.Count != 0)
        {
            if (visitedNodes.Any(n => n == node))
            {
                return true;
            }

            Node[] newVisitedNodes = [.. visitedNodes, node];

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

        Func<IReadOnlyList<string>, bool> dependencyActivator = dependencies =>
        {
            var dependentFeaturesToActivate = new List<FeatureInfo>();

            foreach (var dependency in dependencies.Select(dependencyName => featuresToActivate
                .SingleOrDefault(f => f.Feature.Name == dependencyName))
                .Where(dependency => dependency != null))
            {
                dependentFeaturesToActivate.Add(dependency!);
            }
            return dependentFeaturesToActivate.Aggregate(false, (current, f) => current | ActivateFeature(f, featuresToActivate, featureConfigurationContext));
        };
        var featureType = featureInfo.Feature.GetType();
        if (featureInfo.Diagnostics.Dependencies.All(dependencyActivator))
        {
            featureInfo.Diagnostics.DependenciesAreMet = true;

            if (!HasAllPrerequisitesSatisfied(featureInfo.Feature, featureInfo.Diagnostics, featureConfigurationContext))
            {
                settings.MarkFeatureAsDeactivated(featureType);
                return false;
            }
            settings.MarkFeatureAsActive(featureType);

            featureInfo.InitializeFrom(featureConfigurationContext);

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

    readonly List<FeatureInfo> enabledFeatures = [];
    readonly Dictionary<string, FeatureInfo> added = [];

    class FeatureInfo(Feature feature, FeatureDiagnosticData diagnostics)
    {
        public FeatureDiagnosticData Diagnostics { get; } = diagnostics;
        public Feature Feature { get; } = feature;
        public IReadOnlyList<FeatureStartupTaskController> TaskControllers => taskControllers;

        public void InitializeFrom(FeatureConfigurationContext featureConfigurationContext)
        {
            Feature.SetupFeature(featureConfigurationContext);
            var featureStartupTasks = new List<string>();
            foreach (var controller in featureConfigurationContext.TaskControllers)
            {
                taskControllers.Add(controller);
                featureStartupTasks.Add(controller.Name);
            }
            Diagnostics.StartupTasks = featureStartupTasks;
            Diagnostics.Active = true;
        }

        public override string ToString() => $"{Feature.Name} [{Feature.Version}]";

        readonly List<FeatureStartupTaskController> taskControllers = [];
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

        internal required FeatureInfo FeatureState;
        internal readonly List<Node> previous = [];
        bool visited;
    }
}