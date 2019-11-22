namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Unicast.Messages;

    class EndpointCreator
    {
        EndpointCreator(SettingsHolder settings, HostingComponent.Configuration hostingConfiguration)
        {
            this.settings = settings;
            this.hostingConfiguration = hostingConfiguration;
        }

        public static StartableEndpointWithExternallyManagedContainer CreateWithExternallyManagedContainer(EndpointConfiguration endpointConfiguration, IConfigureComponents externalContainer)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

            var hostingSettings = settings.Get<HostingComponent.Settings>();

            var hostingConfiguration = HostingComponent.PrepareConfiguration(hostingSettings, assemblyScanningComponent, externalContainer);

            if (hostingSettings.CustomObjectBuilder != null)
            {
                throw new InvalidOperationException("An internally managed container has already been configured using 'EndpointConfiguration.UseContainer'. It is not possible to use both an internally managed container and an externally managed container.");
            }

            hostingConfiguration.AddStartupDiagnosticsSection("Container", new
            {
                Type = "external"
            });

            var endpointCreator = new EndpointCreator(settings, hostingConfiguration);

            endpointCreator.Initialize();

            var startableEndpoint = new StartableEndpointWithExternallyManagedContainer(endpointCreator, hostingConfiguration);

            //for backwards compatibility we need to make the IBuilder available in the container
            externalContainer.ConfigureComponent(_ => startableEndpoint.Builder.Value, DependencyLifecycle.SingleInstance);

            return startableEndpoint;
        }

        public static StartableEndpointWithInternallyManagedContainer CreateWithInternallyManagedContainer(EndpointConfiguration endpointConfiguration)
        {
            var settings = endpointConfiguration.Settings;

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            FinalizeConfiguration(endpointConfiguration, assemblyScanningComponent.AvailableTypes);

            var hostingSettting = settings.Get<HostingComponent.Settings>();
            var useDefaultBuilder = hostingSettting.CustomObjectBuilder == null;
            var container = useDefaultBuilder ? new LightInjectObjectBuilder() : hostingSettting.CustomObjectBuilder;

            var commonObjectBuilder = new CommonObjectBuilder(container);

            IConfigureComponents internalContainer = commonObjectBuilder;
            IBuilder internalBuilder = commonObjectBuilder;

            //for backwards compatibility we need to make the IBuilder available in the container
            internalContainer.ConfigureComponent(_ => internalBuilder, DependencyLifecycle.SingleInstance);

            var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, internalContainer);

            if (useDefaultBuilder)
            {
                hostingConfiguration.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });
            }
            else
            {
                var containerType = internalContainer.GetType();

                hostingConfiguration.AddStartupDiagnosticsSection("Container", new
                {
                    Type = containerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(containerType)
                });
            }

            var endpointCreator = new EndpointCreator(settings, hostingConfiguration);

            endpointCreator.Initialize();

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var startableEndpoint = endpointCreator.CreateStartableEndpoint(internalBuilder, hostingComponent);

            hostingComponent.RegisterBuilder(internalBuilder, true);

            return new StartableEndpointWithInternallyManagedContainer(startableEndpoint, hostingComponent);
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

            hostingConfiguration.Container.RegisterSingleton<ReadOnlySettings>(settings);

            featureComponent = new FeatureComponent(settings);

            // This needs to happen here to make sure that features enabled state is present in settings so both
            // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
            featureComponent.RegisterFeatureEnabledStatusInSettings(hostingConfiguration);

            ConfigRunBeforeIsFinalized(hostingConfiguration);

            transportSeam = TransportSeam.Create(settings.Get<TransportSeam.Settings>(), hostingConfiguration);

            var receiveConfiguration = ReceiveComponent.PrepareConfiguration(
                settings.Get<ReceiveComponent.Settings>(),
                transportSeam);

            var routingConfiguration = RoutingComponent.Configure(settings.Get<RoutingComponent.Settings>());

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            recoverabilityComponent = new RecoverabilityComponent(settings);

            var featureConfigurationContext = new FeatureConfigurationContext(settings, hostingConfiguration.Container, pipelineSettings, routingConfiguration, receiveConfiguration);

            featureComponent.Initalize(featureConfigurationContext);

            hostingConfiguration.CreateHostInformationForV7BackwardsCompatibility();

            recoverabilityComponent.Initialize(receiveConfiguration, hostingConfiguration, transportSeam);

            var routingComponent = RoutingComponent.Initialize(
                routingConfiguration,
                transportSeam,
                receiveConfiguration,
                settings.Get<Conventions>(),
                pipelineSettings);

            sendComponent = SendComponent.Initialize(pipelineSettings, hostingConfiguration, routingComponent, messageMapper, transportSeam);

            pipelineComponent = PipelineComponent.Initialize(pipelineSettings, hostingConfiguration);

            hostingConfiguration.Container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            receiveComponent = ReceiveComponent.Initialize(
                receiveConfiguration,
                pipelineComponent,
                settings.ErrorQueueAddress(),
                hostingConfiguration,
                pipelineSettings);

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

        public IStartableEndpoint CreateStartableEndpoint(IBuilder builder, HostingComponent hostingComponent)
        {
            return new StartableEndpoint(settings,
                featureComponent,
                receiveComponent,
                transportSeam.TransportInfrastructure,
                pipelineComponent,
                recoverabilityComponent,
                hostingComponent,
                sendComponent,
                builder);
        }

        void ConfigRunBeforeIsFinalized(HostingComponent.Configuration hostingConfiguration)
        {
            foreach (var instanceToInvoke in hostingConfiguration.AvailableTypes.Where(IsIWantToRunBeforeConfigurationIsFinalized)
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
        ReceiveComponent receiveComponent;
        RecoverabilityComponent recoverabilityComponent;
        HostingComponent.Configuration hostingConfiguration;
        SendComponent sendComponent;
        TransportSeam transportSeam;
    }
}
