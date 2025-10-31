#nullable enable

namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Settings;

class FeatureComponent(FeatureComponent.Settings settings)
{
    public void Initialize(FeatureConfigurationContext featureConfigurationContext, SettingsHolder settings)
    {
        var featureStats = SetupFeatures(featureConfigurationContext, settings);

        settings.AddStartupDiagnosticsSection("Features", featureStats);
    }

    public FeatureDiagnosticData[] SetupFeatures(FeatureConfigurationContext featureConfigurationContext, SettingsHolder settings)
    {
        // featuresToActivate is enumerated twice because after setting defaults some new features might got activated.
        var sourceFeatures = features.Sort();

        while (true)
        {
            var featureToActivate = sourceFeatures.FirstOrDefault(f => f.Enabled);
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

        return [.. features.Select(t => t.Diagnostics)];
    }

    public async Task StartFeatures(IServiceProvider builder, IMessageSession session, CancellationToken cancellationToken = default)
    {
        var startedTaskControllers = new List<FeatureStartupTaskController>();

        // sequential starting of startup tasks is intended, introducing concurrency here could break a lot of features.
        foreach (var feature in enabledFeatures.Where(f => f.IsActive))
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
        var featureStopTasks = enabledFeatures.Where(f => f.IsActive)
            .SelectMany(f => f.TaskControllers)
            .Select(task => task.Stop(session, cancellationToken));

        return Task.WhenAll(featureStopTasks);
    }

    static bool ActivateFeature(FeatureInfo featureInfo, IReadOnlyCollection<FeatureInfo> featuresToActivate,
        FeatureConfigurationContext featureConfigurationContext)
    {
        if (featureInfo.IsActive)
        {
            return true;
        }

        if (featureInfo.DependencyNames.All(dependencyNames =>
                DependencyActivator(dependencyNames, featuresToActivate, featureConfigurationContext)))
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

        static bool DependencyActivator(IReadOnlyCollection<string> dependencyNames,
            IReadOnlyCollection<FeatureInfo> enabledFeatures, FeatureConfigurationContext featureConfigurationContext)
        {
            var dependentFeaturesToActivate = dependencyNames
                .Select(dependencyName => enabledFeatures.SingleOrDefault(f => f.Name == dependencyName))
                .Where(dependency => dependency != null)
                .Select(dependency => dependency!)
                .ToList();

            return dependentFeaturesToActivate.Aggregate(false,
                (current, f) => current | ActivateFeature(f, enabledFeatures, featureConfigurationContext));
        }
    }

    readonly List<FeatureInfo> enabledFeatures = [];
    readonly IReadOnlyCollection<FeatureInfo> features = settings.Features;

    public class Settings(FeatureFactory factory)
    {
        public Settings() : this(new FeatureFactory())
        {
        }

        public IReadOnlyCollection<FeatureInfo> Features => added;

        public void EnableFeature<T>() where T : Feature
        {
            var featureName = Feature.GetFeatureName<T>();
            if (!added.TryGetValue(featureName, out var info))
            {
                info = AddCore(factory.CreateFeature(typeof(T)));
            }
            info.Enable();
        }

        public void DisableFeature<T>() where T : Feature
        {
            var featureName = Feature.GetFeatureName<T>();
            if (!added.TryGetValue(featureName, out var info))
            {
                info = AddCore(factory.CreateFeature(typeof(T)));
            }
            info.Disable();
        }

        public void EnableFeatureByDefault<T>() where T : Feature
        {
            var featureName = Feature.GetFeatureName<T>();
            if (!added.TryGetValue(featureName, out var info))
            {
                info = AddCore(factory.CreateFeature(typeof(T)));
            }
            info.EnableByDefault();
        }

        public bool IsFeature<T>(FeatureState state) where T : Feature
        {
            var featureName = Feature.GetFeatureName<T>();
            if (added.TryGetValue(featureName, out var info))
            {
                return info.In(state);
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
            if (!added.Contains(featureName))
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
            featureInfo = new FeatureInfo(feature, feature.Dependencies.Select(d => d.Select(x => x.FeatureName).ToList().AsReadOnly()).ToList().AsReadOnly());
            added.Add(featureInfo);

            var featuresToEnableByDefault = new List<FeatureInfo>(feature.ToBeEnabledByDefault.Count);
            foreach (var toEnableByDefault in feature.ToBeEnabledByDefault)
            {
                if (!added.TryGetValue(toEnableByDefault.FeatureName, out var info))
                {
                    info = AddCore(factory.CreateFeature(toEnableByDefault.FeatureType));
                }

                featuresToEnableByDefault.Add(info);
            }

            featureInfo.UpdateDependencies(featuresToEnableByDefault);

            foreach (var dependencies in feature.Dependencies)
            {
                foreach ((string featureName, Type? dependentFeatureType) in dependencies)
                {
                    if (added.Contains(featureName))
                    {
                        continue;
                    }

                    // when the feature type is null we assume there is a weak dependency to a feature that was only referenced by
                    // the name but must have been or will be added later to be taken into account by the dependency walking
                    if (dependentFeatureType is not null)
                    {
                        _ = AddCore(factory.CreateFeature(dependentFeatureType));
                    }
                }
            }

            return featureInfo;
        }

        readonly FeatureInfoCollection added = [];

        sealed class FeatureInfoCollection : KeyedCollection<string, FeatureInfo>
        {
            protected override string GetKeyForItem(FeatureInfo item) => item.Name;
        }
    }
}