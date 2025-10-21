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

    public void EnableFeature(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(featureType));
        }

        info.Enable();
    }

    public void DisableFeature(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(featureType));
        }
        info.Disable();
    }

    public void EnableFeatureByDefault(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(featureType));
        }

        info.Enable();
    }

    public void EnableFeatureByDefault<T>() where T : Feature
    {
        var featureName = Feature.GetFeatureName(typeof(T));
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(typeof(T)));
        }

        info.Enable();
    }

    public bool IsFeature(Type featureType, FeatureState state)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (added.TryGetValue(featureName, out var info))
        {
            return info.State == state;
        }
        // backward compat with GetOrDefault
        return state == FeatureState.Disabled;
    }

    public void Add(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.ContainsKey(featureName))
        {
            Add(factory.CreateFeature(featureType));
        }
    }

    public void Add(Feature feature) => _ = AddCore(feature);

    FeatureInfo AddCore(Feature feature)
    {
        if (added.TryGetValue(feature.Name, out var featureInfo))
        {
            return featureInfo;
        }

        var dependencies = feature.Dependencies;
        var dependencyFeatureInfos = new List<FeatureInfo>(dependencies.Count);
        foreach (var dependency in dependencies)
        {
            var featureName = dependency.FeatureType != null ? Feature.GetFeatureName(dependency.FeatureType) : dependency.FeatureName;
            if (!added.TryGetValue(featureName, out var info))
            {
                var dependentFeatureType = dependency.FeatureType ?? Type.GetType(dependency.FeatureName, false);
                if (dependentFeatureType != null)
                {
                    var dependentFeature = factory.CreateFeature(dependentFeatureType);
                    Add(dependentFeature);
                    // we can make all this cleaner
                    info = added[dependentFeature.Name];
                }
            }

            if (info is null)
            {
                continue;
            }

            if (dependency.EnabledByDefault)
            {
                info.MarkAsEnabledByDefault();
            }

            dependencyFeatureInfos.Add(info);
        }

        featureInfo = new FeatureInfo(feature, dependencyFeatureInfos);
        added.Add(feature.Name, featureInfo);
        return featureInfo;
    }

    public FeatureDiagnosticData[] SetupFeatures(FeatureConfigurationContext featureConfigurationContext)
    {
        // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
        var sourceFeatures = Sort(added.Values);

        while (true)
        {
            var featureToActivate = sourceFeatures.FirstOrDefault(x => x.State == FeatureState.Enabled);
            if (featureToActivate == null)
            {
                break;
            }
            sourceFeatures.Remove(featureToActivate);
            enabledFeatures.Add(featureToActivate);
            featureToActivate.Configure(settings);
        }

        foreach (var feature in enabledFeatures)
        {
            _ = ActivateFeature(feature, enabledFeatures, featureConfigurationContext);
        }

        return [.. added.Values.Select(t => t.Diagnostics)];
    }

    public async Task StartFeatures(IServiceProvider builder, IMessageSession session, CancellationToken cancellationToken = default)
    {
        var startedTaskControllers = new List<FeatureStartupTaskController>();

        // sequential starting of startup tasks is intended, introducing concurrency here could break a lot of features.
        foreach (var feature in enabledFeatures.Where(f => f.State == FeatureState.Active))
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
        var featureStopTasks = enabledFeatures.Where(f => f.State == FeatureState.Active)
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
            foreach (var featureInfo in node.FeatureState.Dependencies)
            {
                if (nameToNodeDict.TryGetValue(featureInfo.Feature.Name, out var referencedNode))
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

    static bool ActivateFeature(FeatureInfo featureInfo, IReadOnlyCollection<FeatureInfo> featuresToActivate, FeatureConfigurationContext featureConfigurationContext)
    {
        if (featureInfo.State == FeatureState.Active)
        {
            return true;
        }

        if (featureInfo.Dependencies.All(info => DependencyActivator(info, featuresToActivate, featureConfigurationContext)))
        {
            featureInfo.Diagnostics.DependenciesAreMet = true;

            if (!HasAllPrerequisitesSatisfied(featureInfo.Feature, featureInfo.Diagnostics, featureConfigurationContext))
            {
                featureInfo.Disable();
                return false;
            }

            featureInfo.Activate();

            featureInfo.InitializeFrom(featureConfigurationContext);

            // because we reuse the context the task controller list needs to be cleared.
            featureConfigurationContext.TaskControllers.Clear();

            return true;
        }
        featureInfo.Disable();
        featureInfo.Diagnostics.DependenciesAreMet = false;
        return false;

        static bool DependencyActivator(FeatureInfo dependency, IReadOnlyCollection<FeatureInfo> enabledFeatures, FeatureConfigurationContext featureConfigurationContext)
        {
            var dependentFeatureToActivate = enabledFeatures.SingleOrDefault(f => f.Equals(dependency));

            return dependentFeatureToActivate is not null && ActivateFeature(dependentFeatureToActivate, enabledFeatures, featureConfigurationContext);
        }
    }

    static bool HasAllPrerequisitesSatisfied(Feature feature, FeatureDiagnosticData diagnosticData, FeatureConfigurationContext context)
    {
        diagnosticData.PrerequisiteStatus = feature.CheckPrerequisites(context);

        return diagnosticData.PrerequisiteStatus.IsSatisfied;
    }

    readonly List<FeatureInfo> enabledFeatures = [];
    readonly Dictionary<string, FeatureInfo> added = [];

    sealed class FeatureInfo
    {
        public FeatureInfo(Feature feature, IReadOnlyCollection<FeatureInfo> dependencies)
        {
            Dependencies = dependencies;
            Diagnostics = new FeatureDiagnosticData
            {
                EnabledByDefault = feature.IsEnabledByDefault,
                PrerequisiteStatus = new PrerequisiteStatus(),
                Name = feature.Name,
                Version = feature.Version,
                Dependencies = dependencies.Select(f => f.Feature.Name).ToList().AsReadOnly(),
                StartupTasks = []
            };
            Feature = feature;
            EnabledByDefault = feature.IsEnabledByDefault;

            if (EnabledByDefault) // backward compat for reflection based stuff
            {
                Enable();
            }
        }

        public FeatureDiagnosticData Diagnostics { get; }
        public FeatureState State { get; private set; }
        public Feature Feature { get; }
        public IReadOnlyList<FeatureStartupTaskController> TaskControllers => taskControllers;
        public IReadOnlyCollection<FeatureInfo> Dependencies { get; }
        bool EnabledByDefault { get; set; }

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
            Diagnostics.Active = Feature.IsActive;
        }

        public override string ToString() => $"{Feature.Name} [{Feature.Version}]";

        public void Configure(SettingsHolder settings)
        {
            Feature.ConfigureDefaults(settings);
            foreach (var dependency in Dependencies)
            {
                if (dependency.EnabledByDefault)
                {
                    dependency.Enable();
                }
            }
        }

        public void Enable() => State = FeatureState.Enabled;

        public void Disable() => State = FeatureState.Disabled;

        public void MarkAsEnabledByDefault() => EnabledByDefault = true;

        public void Activate() => State = FeatureState.Active;

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