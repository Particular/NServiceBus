namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Unicast.Messages;

    class EndpointCreator
    {
        EndpointCreator(SettingsHolder settings, HostingComponent hostingComponent)
        {
            this.settings = settings;
            this.hostingComponent = hostingComponent;
        }

        public static StartableEndpointWithExternallyManagedContainer CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents configureComponents)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

            var hostingConfiguration = settings.Get<HostingComponent.Configuration>();

            if (hostingConfiguration.CustomContainer != null)
            {
                throw new InvalidOperationException("An internally managed container has already been configured using 'EndpointConfiguration.UseContainer'. It is not possible to use both an internally managed container and an externally managed container.");
            }

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration, assemblyScanningComponent, configureComponents, null);

            hostingComponent.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            var endpointCreator = new EndpointCreator(settings, hostingComponent);
            var startableEndpoint = new StartableEndpointWithExternallyManagedContainer(endpointCreator);

            //for backwards compatibility we need to make the IBuilder available in the container
            configureComponents.ConfigureComponent(_ => startableEndpoint.Builder.Value, DependencyLifecycle.SingleInstance);

            endpointCreator.Initialize();

            return startableEndpoint;
        }

        public static Task<IStartableEndpoint> CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

            var hostingConfiguration = settings.Get<HostingComponent.Configuration>();
            var useDefaultBuilder = hostingConfiguration.CustomContainer == null;
            var container = useDefaultBuilder ? new LightInjectObjectBuilder() : hostingConfiguration.CustomContainer;

            var commonObjectBuilder = new CommonObjectBuilder(container);

            IConfigureComponents internalContainer = commonObjectBuilder;
            IBuilder internalBuilder = commonObjectBuilder;

            //for backwards compatibility we need to make the IBuilder available in the container
            internalContainer.ConfigureComponent(_ => internalBuilder, DependencyLifecycle.SingleInstance);

            var hostingComponent = HostingComponent.Initialize(settings.Get<HostingComponent.Configuration>(), assemblyScanningComponent, internalContainer, internalBuilder);

            if (useDefaultBuilder)
            {
                hostingComponent.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });
            }
            else
            {
                var containerType = internalContainer.GetType();

                hostingComponent.AddStartupDiagnosticsSection("Container", new
                {
                    Type = containerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(containerType)
                });
            }

            var endpointCreator = new EndpointCreator(settings, hostingComponent);

            endpointCreator.Initialize();

            return endpointCreator.CreateStartableEndpoint(internalBuilder);
        }

        static void FinalizeConfiguration(EndpointConfiguration endpointConfiguration, List<Type> availableTypes)
        {
            ActivateAndInvoke<INeedInitialization>(availableTypes, t => t.Customize(endpointConfiguration));

            var conventions = endpointConfiguration.ConventionsBuilder.Conventions;
            endpointConfiguration.Settings.SetDefault(conventions);

            ConfigureMessageTypes(conventions, endpointConfiguration.Settings);
        }

        void Initialize()
        {
            var pipelineSettings = settings.Get<PipelineSettings>();

            hostingComponent.Container.RegisterSingleton<ReadOnlySettings>(settings);

            featureComponent = new FeatureComponent(settings);

            // This needs to happen here to make sure that features enabled state is present in settings so both
            // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
            featureComponent.RegisterFeatureEnabledStatusInSettings(hostingComponent);

            ConfigRunBeforeIsFinalized(hostingComponent);

            transportComponent = TransportComponent.Initialize(settings.Get<TransportComponent.Configuration>(), settings, containerComponent);

            var receiveConfiguration = BuildReceiveConfiguration(transportComponent);

            var routingComponent = RoutingComponent.Initialize(
                settings.Get<RoutingComponent.Configuration>(),
                transportComponent,
                receiveConfiguration,
                settings.Get<Conventions>(),
                pipelineSettings);

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            recoverabilityComponent = new RecoverabilityComponent(settings);

            var featureConfigurationContext = new FeatureConfigurationContext(settings, hostingComponent.Container, pipelineSettings, routingComponent, receiveConfiguration, recoverabilityComponent);

            featureComponent.Initalize(featureConfigurationContext);

            hostingComponent.CreateHostInformationForV7BackwardsCompatibility();

            recoverabilityComponent.Initialize(receiveConfiguration, hostingComponent);

            sendComponent = SendComponent.Initialize(pipelineSettings, hostingComponent, routingComponent, messageMapper);

            pipelineComponent = PipelineComponent.Initialize(pipelineSettings, hostingComponent);

            hostingComponent.Container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            receiveComponent = ReceiveComponent.Initialize(
                settings.Get<ReceiveComponent.Configuration>(),
                receiveConfiguration,
                transportComponent,
                pipelineComponent,
                settings.ErrorQueueAddress(),
                hostingComponent,
                pipelineSettings);

            installationComponent = InstallationComponent.Initialize(settings.Get<InstallationComponent.Configuration>(),
                hostingComponent,
                transportComponent);

            // The settings can only be locked after initializing the feature component since it uses the settings to store & share feature state.
            // As well as all the other components have been initialized
            settings.PreventChanges();

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
                featureComponent,
                transportComponent,
                receiveComponent,
                pipelineComponent,
                recoverabilityComponent,
                hostingComponent,
                sendComponent,
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

        void ConfigRunBeforeIsFinalized(HostingComponent hostingComponent)
        {
            foreach (var instanceToInvoke in hostingComponent.AvailableTypes.Where(IsIWantToRunBeforeConfigurationIsFinalized)
                .Select(type => (IWantToRunBeforeConfigurationIsFinalized)Activator.CreateInstance(type)))
            {
                instanceToInvoke.Run(settings);
            }
        }

        static bool IsIWantToRunBeforeConfigurationIsFinalized(Type type)
        {
            return typeof(IWantToRunBeforeConfigurationIsFinalized).IsAssignableFrom(type);
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
        FeatureComponent featureComponent;
        TransportComponent transportComponent;
        ReceiveComponent receiveComponent;
        RecoverabilityComponent recoverabilityComponent;
        InstallationComponent installationComponent;
        HostingComponent hostingComponent;
        SendComponent sendComponent;
    }
}
