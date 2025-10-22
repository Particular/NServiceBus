#nullable enable

namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Settings;

class FeatureComponent(FeatureFactory factory) // for testing
{
    public FeatureComponent() : this(new FeatureFactory())
    {
    }

    internal List<FeatureDiagnosticData> Status => [.. added.Values.Select(f => f.Diagnostics)];

    public void Initialize(FeatureConfigurationContext featureConfigurationContext, SettingsHolder settings)
    {
        var featureStats = SetupFeatures(featureConfigurationContext, settings);

        settings.AddStartupDiagnosticsSection("Features", featureStats);
    }

    public void EnableFeature(Type featureType)
    {
        var featureName = Feature.GetFeatureName(featureType);
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(featureType));
        }

        info.Enable();
    }

    public void EnableFeature<T>() where T : Feature
    {
        var featureName = Feature.GetFeatureName(typeof(T));
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(typeof(T)));
        }
        info.Enable();
    }

    public void DisableFeature<T>() where T : Feature
    {
        var featureName = Feature.GetFeatureName(typeof(T));
        if (!added.TryGetValue(featureName, out var info))
        {
            info = AddCore(factory.CreateFeature(typeof(T)));
        }
        info.Disable();
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

    public void EnableFeatureByDefault(Type featureType) => EnableFeature(featureType);

    public void EnableFeatureByDefault<T>() where T : Feature => EnableFeature<T>();

    public bool IsFeature<T>(FeatureState state) where T : Feature
    {
        var featureName = Feature.GetFeatureName(typeof(T));
        if (added.TryGetValue(featureName, out var info))
        {
            return info.State == state;
        }
        // backward compat with GetOrDefault
        return state == FeatureState.Disabled;
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

    public void AddScannedTypes(IEnumerable<Type> availableTypes)
    {
        foreach (var featureType in availableTypes.Where(IsFeature))
        {
            Add(featureType);
        }
    }

    static bool IsFeature(Type type) => typeof(Feature).IsAssignableFrom(type);

    void Add(Type featureType)
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

        // The actual list of dependency names can be different from the found hard-wired dependencies due to the DependsOn allowing to do weak typing.
        featureInfo = new FeatureInfo(feature, feature.Dependencies.Select(d => d.FeatureName).ToList().AsReadOnly());
        added.Add(featureInfo.Name, featureInfo);

        var dependencies = feature.Dependencies;
        var dependencyFeatureInfos = new List<FeatureInfo>(dependencies.Count);
        foreach (var dependency in dependencies)
        {
            if (!added.TryGetValue(dependency.FeatureName, out var info))
            {
                var dependentFeatureType = dependency.FeatureType;
                // when the feature type is null we assume there is a weak dependency to a feature that was only referenced by
                // the name but must have been or will be added later to be taken into account by the dependency walking
                if (dependentFeatureType is not null)
                {
                    info = AddCore(factory.CreateFeature(dependentFeatureType));
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

        featureInfo.UpdateDependencies(dependencyFeatureInfos);
        return featureInfo;
    }

    public FeatureDiagnosticData[] SetupFeatures(FeatureConfigurationContext featureConfigurationContext, SettingsHolder settings)
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

    static List<FeatureInfo> Sort(IReadOnlyCollection<FeatureInfo> featureInfos)
    {
        // Step 1: create nodes for graph
        var nameToNodeDict = new Dictionary<string, Node>();
        var allNodes = new List<Node>(featureInfos.Count);
        foreach (var featureInfo in featureInfos)
        {
            // create entries to preserve order within
            var node = new Node(featureInfo);

            nameToNodeDict[featureInfo.Name] = node;
            allNodes.Add(node);
        }

        // Step 2: create edges dependencies
        foreach (var node in allNodes)
        {
            foreach (var dependencyName in node.Dependencies)
            {
                if (nameToNodeDict.TryGetValue(dependencyName, out var referencedNode))
                {
                    node.Previous.Add(referencedNode);
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
        if (node.Previous.Count == 0)
        {
            return false;
        }

        if (visitedNodes.Any(n => n == node))
        {
            return true;
        }

        Node[] newVisitedNodes = [.. visitedNodes, node];
        foreach (var subNode in node.Previous)
        {
            if (DirectedCycleExistsFrom(subNode, newVisitedNodes))
            {
                return true;
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

        if (featureInfo.DependencyNames.All(dependencyName => DependencyActivator(dependencyName, featuresToActivate, featureConfigurationContext)))
        {
            featureInfo.Diagnostics.DependenciesAreMet = true;

            if (!featureInfo.HasAllPrerequisitesSatisfied(featureConfigurationContext))
            {
                featureInfo.Deactivate();
                return false;
            }

            featureInfo.Activate();

            featureInfo.InitializeFrom(featureConfigurationContext);

            // because we reuse the context the task controller list needs to be cleared.
            featureConfigurationContext.TaskControllers.Clear();

            return true;
        }
        featureInfo.Deactivate();
        featureInfo.Diagnostics.DependenciesAreMet = false;
        return false;

        static bool DependencyActivator(string dependencyName, IReadOnlyCollection<FeatureInfo> enabledFeatures, FeatureConfigurationContext featureConfigurationContext)
        {
            var dependentFeatureToActivate = enabledFeatures.SingleOrDefault(f => f.Name == dependencyName);

            return dependentFeatureToActivate is not null && ActivateFeature(dependentFeatureToActivate, enabledFeatures, featureConfigurationContext);
        }
    }

    readonly List<FeatureInfo> enabledFeatures = [];
    readonly Dictionary<string, FeatureInfo> added = [];

    sealed class FeatureInfo
    {
        public FeatureInfo(Feature feature, IReadOnlyCollection<string> dependencyNames)
        {
            DependencyNames = dependencyNames;
            Diagnostics = new FeatureDiagnosticData
            {
                EnabledByDefault = feature.IsEnabledByDefault,
                PrerequisiteStatus = new PrerequisiteStatus(),
                Name = feature.Name,
                Version = feature.Version,
                Dependencies = dependencyNames,
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
        public string Name => Feature.Name;

        public IReadOnlyList<FeatureStartupTaskController> TaskControllers => taskControllers;
        public IReadOnlyCollection<string> DependencyNames { get; }

        Feature Feature { get; }
        IReadOnlyCollection<FeatureInfo> Dependencies { get; set; } = [];
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

        public bool HasAllPrerequisitesSatisfied(FeatureConfigurationContext featureConfigurationContext)
        {
            Diagnostics.PrerequisiteStatus = Feature.CheckPrerequisites(featureConfigurationContext);

            return Diagnostics.PrerequisiteStatus.IsSatisfied;
        }

        public void Enable() => State = FeatureState.Enabled;

        public void Disable() => State = FeatureState.Disabled;

        public void MarkAsEnabledByDefault() => EnabledByDefault = true;

        public void Activate() => State = FeatureState.Active;

        public void Deactivate() => State = FeatureState.Deactivated;

        public void UpdateDependencies(IReadOnlyCollection<FeatureInfo> dependencyFeatureInfos) => Dependencies = dependencyFeatureInfos;

        readonly List<FeatureStartupTaskController> taskControllers = [];
    }

    sealed class Node(FeatureInfo featureInfo)
    {
        public IReadOnlyCollection<string> Dependencies => featureInfo.DependencyNames;
        public List<Node> Previous { get; } = [];

        public void Visit(ICollection<FeatureInfo> output)
        {
            if (visited)
            {
                return;
            }
            visited = true;
            foreach (var n in Previous)
            {
                n.Visit(output);
            }
            output.Add(featureInfo);
        }

        bool visited;
    }
}