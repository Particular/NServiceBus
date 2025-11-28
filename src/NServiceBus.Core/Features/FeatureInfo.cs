#nullable enable

namespace NServiceBus.Features;

using System.Collections.Generic;
using Settings;

sealed class FeatureInfo
{
    readonly List<IFeatureStartupTaskController> taskControllers = [];

    public FeatureInfo(Feature feature, IReadOnlyCollection<IReadOnlyCollection<string>> dependencyNames)
    {
        if (feature.IsEnabledByDefault) // backward compat for reflection-based stuff
        {
            Enable();
        }

        DependencyNames = dependencyNames;
        Diagnostics = new FeatureDiagnosticData
        {
            Enabled = Enabled,
            PrerequisiteStatus = new PrerequisiteStatus(),
            Name = feature.Name,
            Version = feature.Version,
            Dependencies = dependencyNames,
            StartupTasks = []
        };
        Feature = feature;
    }

    public FeatureDiagnosticData Diagnostics { get; }
    public string Name => Feature.Name;
    public bool Enabled => State is FeatureState.Enabled;
    public bool IsActive => State is FeatureState.Active;
    public IReadOnlyList<IFeatureStartupTaskController> TaskControllers => taskControllers;
    public IReadOnlyCollection<IReadOnlyCollection<string>> DependencyNames { get; }

    Feature Feature { get; }
    FeatureState State { get; set; }
    IReadOnlyCollection<FeatureInfo> DependenciesToEnable { get; set; } = [];

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

    public bool In(FeatureState state) => State == state;

    public void Configure(SettingsHolder settings)
    {
        Feature.ConfigureDefaults(settings);
        foreach (FeatureInfo dependency in DependenciesToEnable)
        {
            dependency.Enable();
        }
    }

    public bool HasAllPrerequisitesSatisfied(FeatureConfigurationContext featureConfigurationContext)
    {
        Diagnostics.PrerequisiteStatus = Feature.CheckPrerequisites(featureConfigurationContext);

        return Diagnostics.PrerequisiteStatus.IsSatisfied;
    }

    public void Enable() => State = FeatureState.Enabled;

    public void Disable() => State = FeatureState.Disabled;

    public void Activate() => State = FeatureState.Active;

    public void Deactivate() => State = FeatureState.Deactivated;

    public void UpdateDependencies(IReadOnlyCollection<FeatureInfo> dependenciesToEnable)
        => DependenciesToEnable = dependenciesToEnable;
}