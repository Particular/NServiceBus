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
    using NServiceBus.ObjectBuilder;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transport;

    class ConfigurableEndpoint
    {
        public ConfigurableEndpoint(SettingsHolder settings,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent)
        {
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.pipelineComponent = pipelineComponent;
        }

        public ConfiguredExternalContainerEndpoint Configure(IConfigureComponents configureComponents)
        {
            containerComponent.UseExternallyManagedContainer(configureComponents);

            var c = Configure();

            return new ConfiguredExternalContainerEndpoint(c, containerComponent);
        }

        public ConfiguredEndpoint Configure()
        {
            containerComponent.Initialize();
            containerComponent.ContainerConfiguration.RegisterSingleton<ReadOnlySettings>(settings);

            RegisterCriticalErrorHandler();

            var concreteTypes = settings.GetAvailableTypes()
                .Where(IsConcrete)
                .ToList();

            var featureActivator = BuildFeatureActivator(concreteTypes);

            ConfigRunBeforeIsFinalized(concreteTypes);

            var transportInfrastructure = InitializeTransportComponent();

            var receiveConfiguration = BuildReceiveConfiguration(transportInfrastructure);

            var routing = InitializeRouting(transportInfrastructure, receiveConfiguration);

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            pipelineComponent.AddRootContextItem<IMessageMapper>(messageMapper);

            var featureStats = featureActivator.SetupFeatures(containerComponent.ContainerConfiguration, pipelineComponent.PipelineSettings, routing, receiveConfiguration);
            settings.AddStartupDiagnosticsSection("Features", featureStats);

            pipelineComponent.RegisterBehaviorsInContainer(containerComponent.ContainerConfiguration);
            containerComponent.ContainerConfiguration.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            var eventAggregator = new EventAggregator(settings.Get<NotificationSubscriptions>());

            pipelineComponent.AddRootContextItem<IEventAggregator>(eventAggregator);

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (shouldRunInstallers)
            {
                RegisterInstallers(concreteTypes);
            }

            settings.AddStartupDiagnosticsSection("Endpoint",
                new
                {
                    Name = settings.EndpointName(),
                    SendOnly = settings.Get<bool>("Endpoint.SendOnly"),
                    NServiceBusVersion = GitVersionInformation.MajorMinorPatch
                }
            );

            var queueBindings = settings.Get<QueueBindings>();
            var receiveComponent = CreateReceiveComponent(receiveConfiguration, transportInfrastructure, pipelineComponent, queueBindings, eventAggregator);

            return new ConfiguredEndpoint(receiveComponent, queueBindings, featureActivator, transportInfrastructure, criticalError, settings, pipelineComponent, containerComponent);
        }

        RoutingComponent InitializeRouting(TransportInfrastructure transportInfrastructure, ReceiveConfiguration receiveConfiguration)
        {
            // use GetOrCreate to use of instances already created during EndpointConfiguration.
            var routing = new RoutingComponent(
                settings.GetOrCreate<UnicastRoutingTable>(),
                settings.GetOrCreate<DistributionPolicy>(),
                settings.GetOrCreate<EndpointInstances>(),
                settings.GetOrCreate<Publishers>());

            routing.Initialize(settings, transportInfrastructure.ToTransportAddress, pipelineComponent.PipelineSettings, receiveConfiguration);

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
            containerComponent.ContainerConfiguration.RegisterSingleton(criticalError);
        }

        void RegisterInstallers(IEnumerable<Type> concreteTypes)
        {
            foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                containerComponent.ContainerConfiguration.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        PipelineComponent pipelineComponent;
        SettingsHolder settings;
        ContainerComponent containerComponent;
        CriticalError criticalError;
    }
}
