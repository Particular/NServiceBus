namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Generic;
using Configuration.AdvancedExtensibility;
using Features;

/// <summary>
/// Simple feature that allows registration of <see cref="FeatureStartupTask"/> without having to define a <see cref="Feature"/> beforehand.
/// </summary>
sealed class FeatureStartupTaskRunner : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        foreach (var startupTaskRegistration in context.Settings.GetOrDefault<List<Action<FeatureConfigurationContext>>>() ?? [])
        {
            startupTaskRegistration(context);
        }
    }
}

public static class StartupTaskRunnerExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        public void RegisterStartupTask(Func<IServiceProvider, FeatureStartupTask> factory)
        {
            var actions = endpointConfiguration.GetSettings().GetOrCreate<List<Action<FeatureConfigurationContext>>>();

            actions.Add(context => context.RegisterStartupTask(factory));
        }

        public void RegisterStartupTask<TStartupTask>() where TStartupTask : FeatureStartupTask
        {
            var actions = endpointConfiguration.GetSettings().GetOrCreate<List<Action<FeatureConfigurationContext>>>();

            actions.Add(context => context.RegisterStartupTask<TStartupTask>());
        }

        public void RegisterStartupTask<TStartupTask>(TStartupTask startupTask) where TStartupTask : FeatureStartupTask
        {
            var actions = endpointConfiguration.GetSettings().GetOrCreate<List<Action<FeatureConfigurationContext>>>();

            actions.Add(context => context.RegisterStartupTask(startupTask));
        }
    }
}