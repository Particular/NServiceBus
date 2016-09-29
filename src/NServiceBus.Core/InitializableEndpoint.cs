namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;
    using Features;
    using Installation;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Settings;
    using Transport;

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

        public async Task<IStartableEndpoint> Initialize()
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

            container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            await RunInstallers(concreteTypes).ConfigureAwait(false);

            var startableEndpoint = new StartableEndpoint(settings, builder, featureActivator, pipelineConfiguration, new EventAggregator(settings.Get<NotificationSubscriptions>()), transportInfrastructure, criticalError);
            return startableEndpoint;
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
            criticalError = new CriticalError(errorAction);
            container.RegisterSingleton(criticalError);
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
            var b = new CommonObjectBuilder(containerToAdapt);

            builder = b;
            container = b;

            container.ConfigureComponent<IBuilder>(_ => b, DependencyLifecycle.SingleInstance);
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

        async Task RunInstallers(IEnumerable<Type> concreteTypes)
        {
            if (Debugger.IsAttached || settings.GetOrDefault<bool>("Installers.Enable"))
            {
                foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
                {
                    container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
                }

                var username = GetInstallationUserName();
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    await installer.Install(username).ConfigureAwait(false);
                }
            }
        }

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        string GetInstallationUserName()
        {
            string username;
            return settings.TryGet("Installers.UserName", out username)
                ? username
                : WindowsIdentity.GetCurrent().Name;
        }

        IBuilder builder;
        IConfigureComponents container;
        PipelineConfiguration pipelineConfiguration;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;
        CriticalError criticalError;
    }
}