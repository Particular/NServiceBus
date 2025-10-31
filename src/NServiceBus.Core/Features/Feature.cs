#nullable enable

namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Linq;
using Particular.Obsoletes;
using Settings;

/// <summary>
/// Used to control the various features supported by the framework.
/// </summary>
public abstract partial class Feature
{
    /// <summary>
    /// Creates an instance of <see cref="Feature" />.
    /// </summary>
    protected Feature() => Name = GetFeatureName(GetType());

    /// <summary>
    /// Feature name.
    /// </summary>
    public string Name { get; internal init; }

    /// <summary>
    /// The version for this feature.
    /// </summary>
    public string Version => FileVersionRetriever.GetFileVersion(GetType());

    /// <summary>
    /// The list of features that this feature is depending on.
    /// </summary>
    internal IReadOnlyCollection<IReadOnlyCollection<Dependency>> Dependencies => dependencies;

    /// <summary>
    /// The list of features that this feature enables by default.
    /// </summary>
    internal IReadOnlyCollection<EnabledByDefault> ToBeEnabledByDefault => toBeEnabledByDefault;

    /// <summary>
    /// Tells if this feature is enabled by default.
    /// </summary>
    public bool IsEnabledByDefault { get; private set; }

    /// <summary>
    /// Indicates that the feature is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Registers default settings.
    /// </summary>
    /// <param name="settings">The settings holder.</param>
    protected void Defaults(Action<SettingsHolder> settings) => registeredDefaults.Add(settings);

    /// <summary>
    /// Called when the features is activated.
    /// </summary>
    protected abstract void Setup(FeatureConfigurationContext context);

    /// <summary>
    /// Adds a setup prerequisite condition. If false this feature won't be setup.
    /// Prerequisites are only evaluated if the feature is enabled.
    /// </summary>
    /// <param name="condition">Condition that must be met in order for this feature to be activated.</param>
    /// <param name="description">Explanation of what this prerequisite checks.</param>
    protected void Prerequisite(Func<FeatureConfigurationContext, bool> condition, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        setupPrerequisites.Add(new SetupPrerequisite
        {
            Condition = condition,
            Description = description
        });
    }

    /// <summary>
    /// Marks this feature as enabled by default.
    /// </summary>
    [ObsoleteMetadata(Message = "Should this feature still need to rely on being discovered via assembly scanning it is possible to suppress this warning. In all other cases it is recommended to create an extension on top of EndpointConfiguration and enable the feature within the extension. This will remove the necessity for the feature to be scanned.", TreatAsErrorFromVersion = "12", RemoveInVersion = "13")]
    [Obsolete("Should this feature still need to rely on being discovered via assembly scanning it is possible to suppress this warning. In all other cases it is recommended to create an extension on top of EndpointConfiguration and enable the feature within the extension. This will remove the necessity for the feature to be scanned.. Will be treated as an error from version 12.0.0. Will be removed in version 13.0.0.", false)]
    protected void EnableByDefault() => IsEnabledByDefault = true;

    /// <summary>
    /// Marks that this feature enables another feature by default.
    /// </summary>
    protected void EnableByDefault<T>() where T : Feature =>
        toBeEnabledByDefault.Add(Default<T>());

    /// <summary>
    /// Registers this feature as depending on the given feature. This means that this feature won't be activated unless
    /// the dependent feature is active.
    /// This also causes this feature to be activated after the other feature.
    /// </summary>
    /// <typeparam name="T">Feature that this feature depends on.</typeparam>
    protected void DependsOn<T>() where T : Feature =>
        dependencies.Add([Depends<T>()]);

    /// <summary>
    /// Registers this feature as depending on the given feature. This means that this feature won't be activated unless
    /// the dependent feature is active. This also causes this feature to be activated after the other feature.
    /// </summary>
    /// <param name="featureTypeName">The <see cref="Type.FullName"/> of the feature that this feature depends on.</param>
    protected void DependsOn(string featureTypeName) =>
        dependencies.Add([new Dependency(featureTypeName)]);

    /// <summary>
    /// Register this feature as depending on at least on of the given features. This means that this feature won't be
    /// activated unless at least one of the provided features in the list is active.
    /// This also causes this feature to be activated after the other features.
    /// </summary>
    /// <param name="features">Features list that this feature require at least one of to be activated.</param>
    protected void DependsOnAtLeastOne(params Type[] features)
    {
        ArgumentNullException.ThrowIfNull(features);

        dependencies.Add([.. features.Select(Depends)]);
    }

    /// <summary>
    /// Registers this feature as optionally depending on the given feature. It means that the declaring feature's
    /// <see cref="Setup" /> method will be called
    /// after the dependent feature's <see cref="Setup" /> if that dependent feature is enabled.
    /// </summary>
    /// <param name="featureName">The name of the feature that this feature depends on.</param>
    protected void DependsOnOptionally(string featureName) => DependsOnAtLeastOne(rootFeature.FeatureName, featureName);

    /// <summary>
    /// Registers this feature as optionally depending on the given feature. It means that the declaring feature's
    /// <see cref="Setup" /> method will be called
    /// after the dependent feature's <see cref="Setup" /> if that dependent feature is enabled.
    /// </summary>
    /// <typeparam name="T">The type of the feature that this feature depends on.</typeparam>
    protected void DependsOnOptionally<T>() where T : Feature => dependencies.Add([rootFeature, Depends<T>()]);

    /// <summary>
    /// Register this feature as depending on at least on of the given features. This means that this feature won't be
    /// activated unless at least one of the provided features in the list is active.
    /// This also causes this feature to be activated after the other features.
    /// </summary>
    /// <param name="featureNames">The name of the features that this feature depends on.</param>
    protected void DependsOnAtLeastOne(params string[] featureNames)
    {
        ArgumentNullException.ThrowIfNull(featureNames);

        dependencies.Add([.. featureNames.Select(n => new Dependency(n))]);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString() => $"{Name} [{Version}]";

    internal PrerequisiteStatus CheckPrerequisites(FeatureConfigurationContext context)
    {
        var status = new PrerequisiteStatus();

        foreach (var prerequisite in setupPrerequisites)
        {
            if (!prerequisite.Condition(context))
            {
                status.ReportFailure(prerequisite.Description);
            }
        }

        return status;
    }

    internal void SetupFeature(FeatureConfigurationContext config)
    {
        Setup(config);

        IsActive = true;
    }

    internal void ConfigureDefaults(SettingsHolder settings)
    {
        foreach (var registeredDefault in registeredDefaults)
        {
            registeredDefault(settings);
        }
    }

    internal static string GetFeatureName<TFeature>() where TFeature : Feature
        => typeof(TFeature).FullName!;

    internal static string GetFeatureName(Type featureType) => featureType.FullName!;

    static EnabledByDefault Default<T>() where T : Feature => new(GetFeatureName<T>(), typeof(T));

    static Dependency Depends<T>() where T : Feature => new(GetFeatureName<T>(), typeof(T));

    static Dependency Depends(Type featureType) => !featureType.IsSubclassOf(baseFeatureType) ? throw new ArgumentException($"A Feature can only depend on another Feature. '{featureType.FullName}' is not a Feature", nameof(featureType)) : new Dependency(GetFeatureName(featureType), featureType);

    readonly List<Action<SettingsHolder>> registeredDefaults = [];
    readonly List<SetupPrerequisite> setupPrerequisites = [];
    readonly List<List<Dependency>> dependencies = [];
    readonly List<EnabledByDefault> toBeEnabledByDefault = [];

    static readonly Type baseFeatureType = typeof(Feature);

    static readonly Dependency rootFeature = Depends<RootFeature>();

    internal readonly record struct Dependency(string FeatureName, Type? FeatureType = null);
    internal readonly record struct EnabledByDefault(string FeatureName, Type FeatureType);

    class SetupPrerequisite
    {
        public required Func<FeatureConfigurationContext, bool> Condition;
        public required string Description;
    }
}