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
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transport;

    class ConfigurableEndpoint
    {
        public ConfigurableEndpoint(SettingsHolder settings,
            IConfigureComponents configurator,
            PipelineSettings pipelineSettings,
            PipelineConfiguration pipelineConfiguration)
        {
            this.settings = settings;
            this.pipelineSettings = pipelineSettings;
            this.pipelineConfiguration = pipelineConfiguration;

            this.configurator = configurator;
            this.configurator.RegisterSingleton(this);
            this.configurator.RegisterSingleton<ReadOnlySettings>(settings);
        }

        public IConfiguredEndpoint Configure()
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

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            var featureStats = featureActivator.SetupFeatures(configurator, pipelineSettings, routing, receiveConfiguration);
            settings.AddStartupDiagnosticsSection("Features", featureStats);

            pipelineConfiguration.RegisterBehaviorsInContainer(configurator);

            configurator.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            var eventAggregator = new EventAggregator(settings.Get<NotificationSubscriptions>());
            var queueBindings = settings.Get<QueueBindings>();

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");
            if (shouldRunInstallers)
            {
                ConfigureInstallers(concreteTypes);
            }

            settings.AddStartupDiagnosticsSection("Endpoint",
                new
                {
                    Name = settings.EndpointName(),
                    SendOnly = settings.Get<bool>("Endpoint.SendOnly"),
                    NServiceBusVersion = GitFlowVersion.MajorMinorPatch
                }
            );

            var builderReference = new BuilderReference();
            configurator.ConfigureComponent(_ => builderReference.GetBuilder(), DependencyLifecycle.SingleInstance);

            var configuredEndpoint = new ConfiguredEndpoint(settings, featureActivator, transportInfrastructure, receiveConfiguration, criticalError, queueBindings, eventAggregator, messageMapper, pipelineConfiguration);

            return configuredEndpoint;
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
            configurator.RegisterSingleton(criticalError);
        }
        
        void ConfigureInstallers(IEnumerable<Type> concreteTypes)
        {
            foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                configurator.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        IConfigureComponents configurator;
        PipelineConfiguration pipelineConfiguration;
        PipelineSettings pipelineSettings;
        SettingsHolder settings;
        CriticalError criticalError;
    }
}
