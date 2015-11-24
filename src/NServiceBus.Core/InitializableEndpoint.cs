namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Features;
    using NServiceBus.Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class InitializableEndpoint : IInitializableEndpoint
    {
        public InitializableEndpoint(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations, PipelineSettings pipelineSettings, PipelineConfiguration pipelineConfiguration, IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables)
        {
            this.settings = settings;
            this.pipelineSettings = pipelineSettings;
            this.pipelineConfiguration = pipelineConfiguration;
            this.startables = startables;

            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            this.container.RegisterSingleton(this);
            this.container.RegisterSingleton<ReadOnlySettings>(settings);
        }

        public Task<IStartableEndpoint> Initialize()
        {
            WireUpConfigSectionOverrides();

            var featureActivator = new FeatureActivator(settings);
            container.RegisterSingleton(featureActivator);

            RegisterCriticalErrorHandler();

            ForAllTypes<Feature>(TypesToScan, t => featureActivator.Add(t.Construct<Feature>()));

            ForAllTypes<IWantToRunWhenBusStartsAndStops>(TypesToScan, t => container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            ActivateAndInvoke<IFinalizeConfiguration>(TypesToScan, t => t.Run(settings));

            var featureStats = featureActivator.SetupFeatures(new FeatureConfigurationContext(settings, container, pipelineSettings));

            pipelineConfiguration.RegisterBehaviorsInContainer(settings, container);

            container.RegisterSingleton(featureStats);

            featureActivator.RegisterStartupTasks(container);

            ReportFeatures(featureStats);
            WireUpInstallers();

            var startableEndpoint = new StartableEndpoint(settings, builder, featureActivator, pipelineConfiguration, startables);
            return Task.FromResult<IStartableEndpoint>(startableEndpoint);
        }

        IList<Type> TypesToScan => settings.GetAvailableTypes();

        void RegisterCriticalErrorHandler()
        {
            Func<IEndpointInstance, string, Exception, Task> errorAction;
            settings.TryGet("onCriticalErrorAction", out errorAction);
            container.ConfigureComponent(() => new CriticalError(errorAction), DependencyLifecycle.SingleInstance);
        }

        void RunUserRegistrations(List<Action<IConfigureComponents>> registrations)
        {
            foreach (var registration in registrations)
            {
                registration(container);
            }
        }

        void RegisterContainerAdapter(IContainer containerToAdapt)
        {
            var b = new CommonObjectBuilder
            {
                Container = containerToAdapt,
            };

            builder = b;
            container = b;

            container.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, containerToAdapt);
        }

        void WireUpConfigSectionOverrides()
        {
            foreach (var t in TypesToScan.Where(t => t.GetInterfaces().Any(IsGenericConfigSource)))
            {
                container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
            }
        }

        void WireUpInstallers()
        {
            foreach (var installerType in TypesToScan.Where(t => typeof(IInstall).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        static void ReportFeatures(FeaturesReport featureStats)
        {
            var reporter = new DisplayDiagnosticsForFeatures();
            reporter.Run(featureStats);
        }

        static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
            {
                action(type);
            }
            // ReSharper restore HeapView.SlowDelegateCreation
        }

        static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
            });
        }

        static bool IsGenericConfigSource(Type t)
        {
            if (!t.IsGenericType)
            {
                return false;
            }

            var args = t.GetGenericArguments();
            if (args.Length != 1)
            {
                return false;
            }

            return typeof(IProvideConfiguration<>).MakeGenericType(args).IsAssignableFrom(t);
        }

        SettingsHolder settings;
        IBuilder builder;
        IConfigureComponents container;
        PipelineSettings pipelineSettings;
        PipelineConfiguration pipelineConfiguration;
        readonly IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables;
    }
}
