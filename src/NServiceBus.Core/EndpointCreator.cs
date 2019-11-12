namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Features;
    using Hosting.Helpers;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Unicast.Messages;

    class EndpointCreator
    {
        EndpointCreator(SettingsHolder settings,
            ContainerComponent containerComponent)
        {
            this.settings = settings;
            this.containerComponent = containerComponent;
        }

        public static StartableEndpointWithExternallyManagedContainer CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents configureComponents)
        {
            FinalizeConfiguration(endpointConfiguration);

            var containerComponent = endpointConfiguration.ContainerComponent;

            containerComponent.InitializeWithExternallyManagedContainer(configureComponents);

            var endpointCreator = new EndpointCreator(endpointConfiguration.Settings, containerComponent);
            var startableEndpoint = new StartableEndpointWithExternallyManagedContainer(endpointCreator);

            //for backwards compatibility we need to make the IBuilder available in the container
            //TODO: will reeable once we see that tests fail
            //containerComponent.ContainerConfiguration.ConfigureComponent(_ => startableEndpoint.Builder.Value, DependencyLifecycle.SingleInstance);

            endpointCreator.Initialize();

            return startableEndpoint;
        }

        public static Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            FinalizeConfiguration(endpointConfiguration);

            var containerComponent = endpointConfiguration.ContainerComponent;

            var internalBuilder = containerComponent.InitializeWithInternallyManagedContainer();

            //for backwards compatibility we need to make the IBuilder available in the container
            //TODO: will reeable once we see that tests fail
            //containerComponent.ContainerConfiguration.ConfigureComponent(_ => internalBuilder, DependencyLifecycle.SingleInstance);

            var endpointCreator = new EndpointCreator(endpointConfiguration.Settings, endpointConfiguration.ContainerComponent);

            endpointCreator.Initialize();

            return endpointCreator.CreateStartableEndpoint(internalBuilder);
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

            featureComponent = new FeatureComponent(settings);

            // This needs to happen here to make sure that features enabled state is present in settings so both
            // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
            featureComponent.RegisterFeatureEnabledStatusInSettings(concreteTypes);

            ConfigRunBeforeIsFinalized(concreteTypes);

            transportComponent = TransportComponent.Initialize(settings.Get<TransportComponent.Configuration>(), settings);

            var receiveConfiguration = BuildReceiveConfiguration(transportComponent);

            var routingComponent = new RoutingComponent(settings);

            var pipelineSettings = settings.Get<PipelineSettings>();
            routingComponent.Initialize(transportComponent, pipelineSettings, receiveConfiguration);

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            recoverabilityComponent = new RecoverabilityComponent(settings);

            var featureConfigurationContext = new FeatureConfigurationContext(settings, containerComponent.ContainerConfiguration, pipelineSettings, routingComponent, receiveConfiguration);

            featureComponent.Initalize(featureConfigurationContext);
            //The settings can only be locked after initializing the feature component since it uses the settings to store & share feature state.
            settings.PreventChanges();

            hostingComponent = HostingComponent.Initialize(settings.Get<HostingComponent.Configuration>(), containerComponent, pipelineSettings);

            recoverabilityComponent.Initialize(receiveConfiguration, hostingComponent);

            pipelineComponent = PipelineComponent.Initialize(pipelineSettings, containerComponent);
            pipelineComponent.AddRootContextItem<IMessageMapper>(messageMapper);

            containerComponent.ContainerConfiguration.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);
            var eventAggregator = new EventAggregator(settings.Get<NotificationSubscriptions>());
            pipelineComponent.AddRootContextItem<IEventAggregator>(eventAggregator);

            receiveComponent = ReceiveComponent.Initialize(receiveConfiguration,
                transportComponent,
                pipelineComponent,
                eventAggregator,
                criticalError,
                settings.ErrorQueueAddress(),
                hostingComponent);

            installationComponent = InstallationComponent.Initialize(settings.Get<InstallationComponent.Configuration>(),
                concreteTypes,
                containerComponent,
                transportComponent);

            settings.AddStartupDiagnosticsSection("Endpoint",
                new
                {
                    Name = settings.EndpointName(),
                    SendOnly = settings.Get<bool>("Endpoint.SendOnly"),
                    NServiceBusVersion = GitVersionInformation.MajorMinorPatch
                }
            );
        }

        public async Task<IStartableEndpoint> CreateStartableEndpoint(IBuilder builder)
        {
            // This is the only component that is started before the user actually calls .Start(). This is due to an old "feature" that allowed users to
            // run installers by "just creating the endpoint". See https://docs.particular.net/nservicebus/operations/installers#running-installers for more details.
            await installationComponent.Start(builder).ConfigureAwait(false);

            return new StartableEndpoint(settings,
                containerComponent,
                featureComponent,
                transportComponent,
                receiveComponent,
                criticalError,
                pipelineComponent,
                recoverabilityComponent,
                hostingComponent,
                builder);
        }

        ReceiveConfiguration BuildReceiveConfiguration(TransportComponent transportComponent)
        {
            var receiveConfiguration = ReceiveConfigurationBuilder.Build(settings, transportComponent);

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

        void RegisterCriticalErrorHandler()
        {
            settings.TryGet("onCriticalErrorAction", out Func<ICriticalErrorContext, Task> errorAction);
            criticalError = new CriticalError(errorAction);
            containerComponent.ContainerConfiguration.RegisterSingleton(criticalError);
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

        PipelineComponent pipelineComponent;
        SettingsHolder settings;
        ContainerComponent containerComponent;
        CriticalError criticalError;
        FeatureComponent featureComponent;
        TransportComponent transportComponent;
        ReceiveComponent receiveComponent;
        RecoverabilityComponent recoverabilityComponent;
        InstallationComponent installationComponent;
        HostingComponent hostingComponent;
    }
}
