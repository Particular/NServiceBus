namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
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

            var featureActivator = BuildFeatureActivator(concreteTypes);

            ConfigRunBeforeIsFinalized(concreteTypes);

            EnsureTransportConfigured();
            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(transportDefinition);
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            // use GetOrCreate to use of instances already created during EndpointConfiguration.
            var routing = new RoutingComponent(
                settings.GetOrCreate<UnicastRoutingTable>(),
                settings.GetOrCreate<DistributionPolicy>(),
                settings.GetOrCreate<EndpointInstances>(),
                settings.GetOrCreate<Publishers>());
            routing.Initialize(settings, transportInfrastructure, pipelineSettings);

            var featureStats = featureActivator.SetupFeatures(container, pipelineSettings, routing);

            pipelineConfiguration.RegisterBehaviorsInContainer(settings, container);

            DisplayDiagnosticsForFeatures.Run(featureStats);

            container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            var receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
            await RunInstallers(concreteTypes, receiveInfrastructure).ConfigureAwait(false);

            var startableEndpoint = new StartableEndpoint(settings, builder, featureActivator, pipelineConfiguration, new EventAggregator(settings.Get<NotificationSubscriptions>()), transportInfrastructure, receiveInfrastructure, criticalError);
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
            foreach (var type in concreteTypes.Where(t => IsFeature(t)))
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

        async Task RunInstallers(IEnumerable<Type> concreteTypes, TransportReceiveInfrastructure receiveInfrastructure)
        {
            if (Debugger.IsAttached || settings.GetOrDefault<bool>("Installers.Enable"))
            {
                foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
                {
                    container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
                }

                var username = GetInstallationUserName();
                await CreateQueuesIfNecessary(receiveInfrastructure, username).ConfigureAwait(false);

                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    await installer.Install(username).ConfigureAwait(false);
                }
            }
        }

        Task CreateQueuesIfNecessary(TransportReceiveInfrastructure receiveInfrastructure, string username)
        {
            if (settings.Get<bool>("Endpoint.SendOnly"))
            {
                return TaskEx.CompletedTask;
            }
            if (!settings.CreateQueues())
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();
            var queueBindings = settings.Get<QueueBindings>();
            
            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        string GetInstallationUserName()
        {
            string username;
            return settings.TryGet("Installers.UserName", out username)
                ? username
                : WindowsIdentity.GetCurrent().Name;
        }

        void EnsureTransportConfigured()
        {
            if (!settings.HasExplicitValue<TransportDefinition>())
            {
                throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.");
            }
        }

        IBuilder builder;
        IConfigureComponents container;
        PipelineConfiguration pipelineConfiguration;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;
        CriticalError criticalError;
    }
}