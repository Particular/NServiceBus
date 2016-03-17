namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;
    using Features;
    using Installation;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Settings;
    using Transports;

    class InitializableEndpoint
    {
        public InitializableEndpoint(SettingsHolder settings, IContainer container, List<Action<IConfigureComponents>> registrations, PipelineSettings pipelineSettings, PipelineConfiguration pipelineConfiguration)
        {
            this.settings = settings;
            this.pipelineSettings = pipelineSettings;
            this.pipelineConfiguration = pipelineConfiguration;

            RegisterContainerAdapter(container);
            RunUserRegistrations(registrations);

            this.container.RegisterSingleton(this);
            this.container.RegisterSingleton<ReadOnlySettings>(settings);
        }

        public Task<IStartableEndpoint> Initialize()
        {
            RegisterCriticalErrorHandler();
            var concreteTypes = settings.GetAvailableTypes()
                .Where(IsConcrete)
                .ToList();
            WireUpConfigSectionOverrides(concreteTypes);

            var featureActivator = BuildFeatureActivator(concreteTypes);

            ConfigRunBeforeIsFinalized(concreteTypes);

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(transportDefinition);
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            var featureStats = featureActivator.SetupFeatures(container, pipelineSettings);

            pipelineConfiguration.RegisterBehaviorsInContainer(settings, container);

            DisplayDiagnosticsForFeatures.Run(featureStats);
            WireUpInstallers(concreteTypes);

            container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            var startableEndpoint = new StartableEndpoint(settings, builder, featureActivator, pipelineConfiguration, new EventAggregator(settings.Get<NotificationSubscriptions>()), transportInfrastructure);
            return Task.FromResult<IStartableEndpoint>(startableEndpoint);
        }

        static bool IsConcrete(Type x)
        {
            return !x.IsAbstract && !x.IsInterface;
        }

        void ConfigRunBeforeIsFinalized(IEnumerable<Type> concreteTypes)
        {
            foreach (var instanceToInvoke in concreteTypes.Where(IsIWantToRunBeforeConfigurationIsFinalized)
                .Select(type => (IWantToRunBeforeConfigurationIsFinalized)Activator.CreateInstance(type)))
            {
                instanceToInvoke.Run(settings);
            }
        }

        static bool IsIWantToRunBeforeConfigurationIsFinalized(Type type)
        {
            return typeof(IWantToRunBeforeConfigurationIsFinalized).IsAssignableFrom(type);
        }

        FeatureActivator BuildFeatureActivator(IEnumerable<Type> concreteTypes)
        {
            var featureActivator = new FeatureActivator(settings);
            foreach (var type in concreteTypes.Where(IsFeature))
            {
                featureActivator.Add(type.Construct<Feature>());
            }
            return featureActivator;
        }

        static bool IsFeature(Type type)
        {
            return typeof(Feature).IsAssignableFrom(type);
        }

        void RegisterCriticalErrorHandler()
        {
            Func<ICriticalErrorContext, Task> errorAction;
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
                Container = containerToAdapt
            };

            builder = b;
            container = b;

            container.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(c => c.Container, containerToAdapt);
        }

        void WireUpConfigSectionOverrides(IEnumerable<Type> concreteTypes)
        {
            foreach (var type in concreteTypes.Where(ImplementsIProvideConfiguration))
            {
                container.ConfigureComponent(type, DependencyLifecycle.InstancePerCall);
            }
        }

        static bool ImplementsIProvideConfiguration(Type type)
        {
            return type.GetInterfaces().Any(IsIProvideConfiguration);
        }

        static bool IsIProvideConfiguration(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var args = type.GetGenericArguments();
            if (args.Length != 1)
            {
                return false;
            }

            return typeof(IProvideConfiguration<>).MakeGenericType(args)
                .IsAssignableFrom(type);
        }

        void WireUpInstallers(IEnumerable<Type> concreteTypes)
        {
            foreach (var installerType in concreteTypes.Where(IsINeedToInstallSomething))
            {
                container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        static bool IsINeedToInstallSomething(Type t)
        {
            return typeof(INeedToInstallSomething).IsAssignableFrom(t);
        }

        IBuilder builder;
        IConfigureComponents container;
        PipelineConfiguration pipelineConfiguration;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;
    }
}