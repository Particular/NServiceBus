namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Features;
    using Hosting.Helpers;
    using Installation;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Settings;
    using Transport;
    using Unicast.Messages;

    class EndpointCreator
    {
        EndpointCreator(SettingsHolder settings,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent)
        {
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.pipelineComponent = pipelineComponent;
        }

        public static StartableEndpointWithExternallyManagedContainer CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents configureComponents)
        {
            FinalizeConfiguration(endpointConfiguration);

            endpointConfiguration.ContainerComponent.InitializeWithExternallyManagedContainer(configureComponents);

            var creator = new EndpointCreator(endpointConfiguration.Settings, endpointConfiguration.ContainerComponent, endpointConfiguration.PipelineComponent);
            creator.Initialize();

            return new StartableEndpointWithExternallyManagedContainer(creator);
        }

        public static Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            FinalizeConfiguration(endpointConfiguration);

            endpointConfiguration.ContainerComponent.InitializeWithInternallyManagedContainer();

            var creator = new EndpointCreator(endpointConfiguration.Settings, endpointConfiguration.ContainerComponent, endpointConfiguration.PipelineComponent);
            creator.Initialize();

            return creator.CreateStartableEndpoint();
        }

        static void FinalizeConfiguration(EndpointConfiguration endpointConfiguration)
        {
            var scannedTypes = PerformAssemblyScanning(endpointConfiguration);

            ActivateAndInvoke<INeedInitialization>(scannedTypes, t => t.Customize(endpointConfiguration));

            var conventions = endpointConfiguration.ConventionsBuilder.Conventions;
            endpointConfiguration.Settings.SetDefault(conventions);

            ConfigureMessageTypes(conventions, endpointConfiguration.Settings);
        }

        void Initialize()
        {
            containerComponent.ContainerConfiguration.RegisterSingleton<ReadOnlySettings>(settings);

            RegisterCriticalErrorHandler();

            var concreteTypes = settings.GetAvailableTypes()
                .Where(IsConcrete)
                .ToList();
  
            ConfigRunBeforeIsFinalized(concreteTypes);

            transportInfrastructure = InitializeTransportComponent();

            var receiveConfiguration = BuildReceiveConfiguration(transportInfrastructure);

            var routingComponent = new RoutingComponent(settings);

            routingComponent.Initialize(transportInfrastructure, pipelineComponent, receiveConfiguration);

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            pipelineComponent.AddRootContextItem<IMessageMapper>(messageMapper);

            recoverabilityComponent = new RecoverabilityComponent(settings);

            var featureConfigurationContext = new FeatureConfigurationContext(settings, containerComponent.ContainerConfiguration, pipelineComponent.PipelineSettings, routingComponent, receiveConfiguration);

            featureComponent = new FeatureComponent(settings);

            //note: This is where the settings gets locked since the feature component uses the settings to store feature state.
            // This locking happes just before the Features gets "setup"
            featureComponent.Initalize(concreteTypes, featureConfigurationContext);

            recoverabilityComponent.Initialize(receiveConfiguration);

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

            queueBindings = settings.Get<QueueBindings>();
            receiveComponent = CreateReceiveComponent(receiveConfiguration, transportInfrastructure, pipelineComponent, queueBindings, eventAggregator);
        }

        public void UseExternallyManagedBuilder(IBuilder builder)
        {
            containerComponent.UseExternallyManagedBuilder(builder);
        }

        public async Task<IStartableEndpoint> CreateStartableEndpoint()
        {
            pipelineComponent.Initialize(containerComponent.Builder);

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (shouldRunInstallers)
            {
                var username = GetInstallationUserName();

                if (settings.CreateQueues())
                {
                    await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
                }

                await RunInstallers(containerComponent.Builder, username).ConfigureAwait(false);
            }

            return new StartableEndpoint(settings, containerComponent, featureComponent, transportInfrastructure, receiveComponent, criticalError, messageSession, recoverabilityComponent);
        }

        async Task RunInstallers(IBuilder builder, string username)
        {
            foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(username).ConfigureAwait(false);
            }
        }

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

        static List<Type> PerformAssemblyScanning(EndpointConfiguration endpointConfiguration)
        {
            var scannedTypes = endpointConfiguration.ScannedTypes;

            if (scannedTypes == null)
            {
                var directoryToScan = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

                scannedTypes = GetAllowedTypes(directoryToScan, endpointConfiguration.Settings);
            }
            else
            {
                scannedTypes = scannedTypes.Union(GetAllowedCoreTypes(endpointConfiguration.Settings)).ToList();
            }

            endpointConfiguration.Settings.SetDefault("TypesToScan", scannedTypes);

            return scannedTypes;
        }

        static List<Type> GetAllowedTypes(string path, SettingsHolder settings)
        {
            var assemblyScannerSettings = settings.GetOrCreate<AssemblyScannerConfiguration>();
            var assemblyScanner = new AssemblyScanner(path)
            {
                AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
                TypesToSkip = assemblyScannerSettings.ExcludedTypes,
                ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
                ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
                ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies
            };

            return Scan(assemblyScanner, settings);
        }

        static List<Type> GetAllowedCoreTypes(SettingsHolder settings)
        {
            var assemblyScannerSettings = settings.GetOrCreate<AssemblyScannerConfiguration>();
            var assemblyScanner = new AssemblyScanner(Assembly.GetExecutingAssembly())
            {
                AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
                TypesToSkip = assemblyScannerSettings.ExcludedTypes,
                ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
                ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
                ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies
            };

            return Scan(assemblyScanner, settings);
        }

        static List<Type> Scan(AssemblyScanner assemblyScanner, SettingsHolder settings)
        {
            var results = assemblyScanner.GetScannableAssemblies();

            settings.AddStartupDiagnosticsSection("AssemblyScanning", new
            {
                Assemblies = results.Assemblies.Select(a => a.FullName),
                results.ErrorsThrownDuringScanning,
                results.SkippedFiles
            });

            return results.Types;
        }

        static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
        {
            ForAllTypes<T>(types, t =>
            {
                if (!HasDefaultConstructor(t))
                {
                    throw new Exception($"Unable to create the type '{t.Name}'. Types implementing '{typeof(T).Name}' must have a public parameterless (default) constructor.");
                }

                var instanceToInvoke = (T)Activator.CreateInstance(t);
                action(instanceToInvoke);
            });
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

        static void ConfigureMessageTypes(Conventions conventions, SettingsHolder settings)
        {
            var messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType);

            messageMetadataRegistry.RegisterMessageTypesFoundIn(settings.GetAvailableTypes());

            settings.Set(messageMetadataRegistry);

            var foundMessages = messageMetadataRegistry.GetAllMessages().ToList();

            settings.AddStartupDiagnosticsSection("Messages", new
            {
                CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
                NumberOfMessagesFoundAtStartup = foundMessages.Count,
                Messages = foundMessages.Select(m => m.MessageType.FullName)
            });
        }

        static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        PipelineComponent pipelineComponent;
        SettingsHolder settings;
        FeatureComponent featureComponent;
        ContainerComponent containerComponent;
        CriticalError criticalError;
        TransportInfrastructure transportInfrastructure;
        QueueBindings queueBindings;
        ReceiveComponent receiveComponent;
        RecoverabilityComponent recoverabilityComponent;
    }
}
