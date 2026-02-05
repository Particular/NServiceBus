#nullable enable

namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal IReadOnlyCollection<IReadOnlyCollection<IDependency>> Dependencies => dependencies;

    /// <summary>
    /// The list of features that this feature enables.
    /// </summary>
    internal IReadOnlyCollection<IEnabled> ToBeEnabled => toBeEnabled;

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
    [ObsoleteMetadata(Message = "In a future version of NServiceBus, Feature classes will not be automatically discovered by runtime assembly scanning. Instead, create an extension method on EndpointConfiguration and enable the feature within the extension method using EnableFeature<T>()", TreatAsErrorFromVersion = "11", RemoveInVersion = "12")]
    [Obsolete("In a future version of NServiceBus, Feature classes will not be automatically discovered by runtime assembly scanning. Instead, create an extension method on EndpointConfiguration and enable the feature within the extension method using EnableFeature<T>(). Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    protected void EnableByDefault() => IsEnabledByDefault = true;

    /// <summary>
    /// Marks that this feature enables another feature.
    /// </summary>
    /// <remarks>This method should be called inside the constructor of the feature.</remarks>
    protected void Enable<TFeature>() where TFeature : Feature, new() =>
        toBeEnabled.Add(Enables<TFeature>());

    /// <summary>
    /// Registers this feature as depending on the given feature. This means that this feature won't be activated unless
    /// the dependent feature is active.
    /// This also causes this feature to be activated after the other feature.
    /// </summary>
    /// <typeparam name="TFeature">Feature that this feature depends on.</typeparam>
    protected void DependsOn<TFeature>() where TFeature : Feature, new() =>
        dependencies.Add([Depends<TFeature>()]);

    /// <summary>
    /// Registers this feature as depending on the given feature. This means that this feature won't be activated unless
    /// the dependent feature is active. This also causes this feature to be activated after the other feature.
    /// </summary>
    /// <param name="featureTypeName">The <see cref="Type.FullName"/> of the feature that this feature depends on.</param>
    protected void DependsOn(string featureTypeName) =>
        dependencies.Add([Depends(featureTypeName)]);

    /// <summary>
    /// Register this feature as depending on at least on of the given features. This means that this feature won't be
    /// activated unless at least one of the provided features in the list is active.
    /// This also causes this feature to be activated after the other features.
    /// </summary>
    /// <param name="features">Features list that this feature require at least one of to be activated.</param>
    [RequiresUnreferencedCode("Feature dependency using types might require access to unreferenced code")]
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
    /// <typeparam name="TFeature">The type of the feature that this feature depends on.</typeparam>
    protected void DependsOnOptionally<TFeature>() where TFeature : Feature, new() => dependencies.Add([rootFeature, Depends<TFeature>()]);

    /// <summary>
    /// Register this feature as depending on at least on of the given features. This means that this feature won't be
    /// activated unless at least one of the provided features in the list is active.
    /// This also causes this feature to be activated after the other features.
    /// </summary>
    /// <param name="featureNames">The name of the features that this feature depends on.</param>
    protected void DependsOnAtLeastOne(params string[] featureNames)
    {
        ArgumentNullException.ThrowIfNull(featureNames);

        dependencies.Add([.. featureNames.Select(Depends)]);
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

    static IEnabled Enables<TFeature>() where TFeature : Feature, new() => Enabled<TFeature>.Instance;
    static IDependency Depends<TFeature>() where TFeature : Feature, new() => Dependency<TFeature>.Instance;
    static IDependency Depends([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Feature)] Type featureType) => !featureType.IsSubclassOf(baseFeatureType) ? throw new ArgumentException($"A Feature can only depend on another Feature. '{featureType.FullName}' is not a Feature", nameof(featureType)) : new TypeDependency(featureType);
    static IDependency Depends(string featureName) => new WeakDependency(featureName);

    readonly List<Action<SettingsHolder>> registeredDefaults = [];
    readonly List<SetupPrerequisite> setupPrerequisites = [];
    readonly List<List<IDependency>> dependencies = [];
    readonly List<IEnabled> toBeEnabled = [];

    static readonly Type baseFeatureType = typeof(Feature);

    static readonly IDependency rootFeature = Depends<RootFeature>();

    internal interface IDependency
    {
        string FeatureName { get; }
        Feature? Create(FeatureFactory factory);
    }

    sealed class Dependency<TFeature> : IDependency where TFeature : Feature, new()
    {
        Dependency()
        {
        }

        public string FeatureName { get; } = GetFeatureName<TFeature>();
        public Feature Create(FeatureFactory factory) => factory.CreateFeature<TFeature>();

        public static readonly IDependency Instance = new Dependency<TFeature>();
    }

    sealed class TypeDependency([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Feature)] Type featureType) : IDependency
    {
        public string FeatureName { get; } = GetFeatureName(featureType);
        public Feature Create(FeatureFactory factory) => factory.CreateFeature(featureType);
    }

    sealed record WeakDependency(string FeatureName) : IDependency
    {
        public Feature? Create(FeatureFactory factory) => null;
    }

    internal interface IEnabled
    {
        string FeatureName { get; }
        Feature Create(FeatureFactory factory);
    }

    sealed class Enabled<TFeature> : IEnabled
        where TFeature : Feature, new()
    {
        Enabled()
        {
        }

        public string FeatureName { get; } = GetFeatureName<TFeature>();
        public Feature Create(FeatureFactory factory) => factory.CreateFeature<TFeature>();

        public static readonly IEnabled Instance = new Enabled<TFeature>();
    }

    class SetupPrerequisite
    {
        public required Func<FeatureConfigurationContext, bool> Condition;
        public required string Description;
    }
}