#nullable enable

namespace NServiceBus.Features;

using System.Collections.Generic;
using Settings;

sealed class FeatureInfo
{
    readonly List<FeatureStartupTaskController> taskControllers = [];

    public FeatureInfo(Feature feature, IReadOnlyCollection<IReadOnlyCollection<string>> dependencyNames)
    {
        if (feature.IsEnabledByDefault) // backward compat for reflection-based stuff
        {
            EnableByDefault();
        }

        DependencyNames = dependencyNames;
        Diagnostics = new FeatureDiagnosticData
        {
            EnabledByDefault = State == FeatureStateInfo.EnabledByDefault,
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
    public bool Enabled => State is FeatureStateInfo.EnabledByDefault or FeatureStateInfo.Enabled;
    public bool IsActive => State is FeatureStateInfo.Active;
    public IReadOnlyList<FeatureStartupTaskController> TaskControllers => taskControllers;
    public IReadOnlyCollection<IReadOnlyCollection<string>> DependencyNames { get; }

    Feature Feature { get; }
    FeatureStateInfo State { get; set; }
    IReadOnlyCollection<FeatureInfo> DependenciesToEnable { get; set; } = [];

    public void InitializeFrom(FeatureConfigurationContext featureConfigurationContext)
    {
        Feature.SetupFeature(featureConfigurationContext);
        var featureStartupTasks = new List<string>();
        foreach (FeatureStartupTaskController controller in featureConfigurationContext.TaskControllers)
        {
            taskControllers.Add(controller);
            featureStartupTasks.Add(controller.Name);
        }

        Diagnostics.StartupTasks = featureStartupTasks;
        Diagnostics.Active = Feature.IsActive;
    }

    public override string ToString() => $"{Feature.Name} [{Feature.Version}]";

    public bool In(FeatureState state) =>
        state switch
        {
            FeatureState.Disabled => State == FeatureStateInfo.Disabled,
            FeatureState.Enabled => State == FeatureStateInfo.Enabled,
            FeatureState.Active => State == FeatureStateInfo.Active,
            FeatureState.Deactivated => State == FeatureStateInfo.Deactivated,
            _ => false
        };

    public void Configure(SettingsHolder settings)
    {
        Feature.ConfigureDefaults(settings);
        foreach (FeatureInfo dependency in DependenciesToEnable)
        {
            dependency.EnableByDefault();
        }
    }

    public bool HasAllPrerequisitesSatisfied(FeatureConfigurationContext featureConfigurationContext)
    {
        Diagnostics.PrerequisiteStatus = Feature.CheckPrerequisites(featureConfigurationContext);

        return Diagnostics.PrerequisiteStatus.IsSatisfied;
    }

    public void Enable() => State = FeatureStateInfo.Enabled;

    public void Disable() => State = FeatureStateInfo.Disabled;

    public void EnableByDefault() => State = FeatureStateInfo.EnabledByDefault;

    public void Activate() => State = FeatureStateInfo.Active;

    public void Deactivate() => State = FeatureStateInfo.Deactivated;

    public void UpdateDependencies(IReadOnlyCollection<FeatureInfo> dependenciesToEnable)
        => DependenciesToEnable = dependenciesToEnable;

    enum FeatureStateInfo
    {
        Disabled,
        Enabled,
        Active,
        Deactivated,
        EnabledByDefault
    }
}