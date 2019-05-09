namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transport;

    class InitializableEndpoint
    {
        public InitializableEndpoint(SettingsHolder settings,
            IContainer container,
            List<Action<IConfigureComponents>> registrations,
            PipelineSettings pipelineSettings,
            PipelineConfiguration pipelineConfiguration)
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

            var transportInfrastructure = InitializeTransportComponent();

            var receiveConfiguration = BuildReceiveConfiguration(transportInfrastructure);

            var routing = InitializeRouting(transportInfrastructure, receiveConfiguration);
            var pipeline = new PipelineComponent(settings, builder);

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            pipeline.AddRootContextItem<IMessageMapper>(messageMapper);

            var featureStats = featureActivator.SetupFeatures(container, pipelineSettings, routing, receiveConfiguration);
            settings.AddStartupDiagnosticsSection("Features", featureStats);

            pipelineConfiguration.RegisterBehaviorsInContainer(container);

            container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            var eventAggregator = new EventAggregator(settings.Get<NotificationSubscriptions>());

            pipeline.AddRootContextItem<IEventAggregator>(eventAggregator);

            pipeline.Initialize();

            var queueBindings = settings.Get<QueueBindings>();

            var receiveComponent = CreateReceiveComponent(receiveConfiguration, transportInfrastructure, pipeline, queueBindings, eventAggregator);

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (shouldRunInstallers)
            {
                var username = GetInstallationUserName();

                if (settings.CreateQueues())
                {
                    await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
                }

                await RunInstallers(concreteTypes, username).ConfigureAwait(false);
            }

            settings.AddStartupDiagnosticsSection("Endpoint",
                new
                {
                    Name = settings.EndpointName(),
                    SendOnly = settings.Get<bool>("Endpoint.SendOnly"),
                    NServiceBusVersion = GitFlowVersion.MajorMinorPatch
                }
            );

            var messageSession = new MessageSession(pipeline.CreateRootContext(builder));

            return new StartableEndpoint(settings, builder, featureActivator, transportInfrastructure, receiveComponent, criticalError, messageSession);
        }

        RoutingComponent InitializeRouting(TransportInfrastructure transportInfrastructure, ReceiveConfiguration receiveConfiguration)
        {
            // use GetOrCreate to use of instances already created during EndpointConfiguration.
            var routing = new RoutingComponent(
                settings.GetOrCreate<UnicastRoutingTable>(),
                settings.GetOrCreate<DistributionPolicy>(),
                settings.GetOrCreate<EndpointInstances>(),
                settings.GetOrCreate<Publishers>());

            routing.Initialize(settings, transportInfrastructure.ToTransportAddress, pipelineSettings, receiveConfiguration);

            return routing;
        }

        TransportInfrastructure InitializeTransportComponent()
        {
            if (!settings.HasExplicitValue<TransportDefinition>())
            {
                throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.");
            }

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError(transportDefinition);
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set(transportInfrastructure);

            var transportType = transportDefinition.GetType();

            settings.AddStartupDiagnosticsSection("Transport", new
            {
                Type = transportType.FullName,
                Version = FileVersionRetriever.GetFileVersion(transportType)
            });

            return transportInfrastructure;
        }

        ReceiveConfiguration BuildReceiveConfiguration(TransportInfrastructure transportInfrastructure)
        {
            var receiveConfiguration = ReceiveConfigurationBuilder.Build(settings, transportInfrastructure);

            if (receiveConfiguration == null)
            {
                return null;
            }

            //note: remove once settings.LogicalAddress() , .LocalAddress() and .InstanceSpecificQueue() has been obsoleted
            settings.Set(receiveConfiguration);

            return receiveConfiguration;
        }

        ReceiveComponent CreateReceiveComponent(ReceiveConfiguration receiveConfiguration,
            TransportInfrastructure transportInfrastructure,
            PipelineComponent pipeline,
            QueueBindings queueBindings,
            EventAggregator eventAggregator)
        {
            var errorQueue = settings.ErrorQueueAddress();

            var receiveComponent = new ReceiveComponent(receiveConfiguration,
                receiveConfiguration != null ? transportInfrastructure.ConfigureReceiveInfrastructure() : null, //don't create the receive infrastructure for send-only endpoints
                pipeline,
                builder,
                eventAggregator,
                criticalError,
                errorQueue);

            receiveComponent.BindQueues(queueBindings);

            if (receiveConfiguration != null)
            {
                settings.AddStartupDiagnosticsSection("Receiving", new
                {
                    receiveConfiguration.LocalAddress,
                    receiveConfiguration.InstanceSpecificQueue,
                    receiveConfiguration.LogicalAddress,
                    receiveConfiguration.PurgeOnStartup,
                    receiveConfiguration.QueueNameBase,
                    TransactionMode = receiveConfiguration.TransactionMode.ToString("G"),
                    receiveConfiguration.PushRuntimeSettings.MaxConcurrency,
                    Satellites = receiveConfiguration.SatelliteDefinitions.Select(s => new
                    {
                        s.Name,
                        s.ReceiveAddress,
                        TransactionMode = s.RequiredTransportTransactionMode.ToString("G"),
                        s.RuntimeSettings.MaxConcurrency
                    }).ToArray()
                });
            }

            return receiveComponent;
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
            settings.TryGet("onCriticalErrorAction", out Func<ICriticalErrorContext, Task> errorAction);
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

        async Task RunInstallers(IEnumerable<Type> concreteTypes, string username)
        {
            foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(username).ConfigureAwait(false);
            }
        }

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        string GetInstallationUserName()
        {
            if (!settings.TryGet("Installers.UserName", out string userName))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    userName = $"{Environment.UserDomainName}\\{Environment.UserName}";
                }
                else
                {
                    userName = Environment.UserName;
                }
            }

            return userName;
        }

        IBuilder builder;
        IConfigureComponents container;
        PipelineConfiguration pipelineConfiguration;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;
        CriticalError criticalError;
    }
}
