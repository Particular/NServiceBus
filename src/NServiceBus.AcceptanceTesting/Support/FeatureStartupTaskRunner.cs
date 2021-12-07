namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Simple feature that allows registration of <see cref="FeatureStartupTask"/> without having to define a <see cref="Feature"/> beforehand.
    /// </summary>
    class FeatureStartupTaskRunner : Feature
    {
        public const string ConfigKey = "FeatureStartupTaskRunner.StartupTasks";
        public FeatureStartupTaskRunner() => EnableByDefault();

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet<List<Func<IServiceProvider, FeatureStartupTask>>>(ConfigKey, out var startupTasks))
            {
                foreach (var startupTaskRegistration in startupTasks)
                {
                    context.RegisterStartupTask(startupTaskRegistration);
                }
            }
        }
    }

    public static class StartupTaskRunnerExtensions
    {
        public static void RegisterStartupTask(this EndpointConfiguration endpointConfiguration, Func<IServiceProvider, FeatureStartupTask> factory)
        {
            var startupTasks = GetStartupTaskRegistrations(endpointConfiguration);

            startupTasks.Add(factory);
        }

        public static void RegisterStartupTask<TStartupTask>(this EndpointConfiguration endpointConfiguration) where TStartupTask : FeatureStartupTask
        {
            var startupTasks = GetStartupTaskRegistrations(endpointConfiguration);

            endpointConfiguration.RegisterComponents(s => s.AddTransient(typeof(TStartupTask)));
            startupTasks.Add(sp => sp.GetRequiredService<TStartupTask>());
        }

        public static void RegisterStartupTask<TStartupTask>(this EndpointConfiguration endpointConfiguration, TStartupTask startupTask) where TStartupTask : FeatureStartupTask
        {
            var startupTasks = GetStartupTaskRegistrations(endpointConfiguration);

            startupTasks.Add(sp => startupTask);
        }

        static List<Func<IServiceProvider, FeatureStartupTask>> GetStartupTaskRegistrations(EndpointConfiguration endpointConfiguration)
        {
            if (!endpointConfiguration.Settings.TryGet<List<Func<IServiceProvider, FeatureStartupTask>>>(FeatureStartupTaskRunner.ConfigKey, out var startupTasks))
            {
                startupTasks = new List<Func<IServiceProvider, FeatureStartupTask>>();
                endpointConfiguration.Settings.Set(FeatureStartupTaskRunner.ConfigKey, startupTasks);
            }

            return startupTasks;
        }
    }
}