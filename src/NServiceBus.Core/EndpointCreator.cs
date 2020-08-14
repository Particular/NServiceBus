namespace NServiceBus
{
    using System;
    using System.Linq;
    using Features;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Pipeline;
    using Settings;
    using Unicast.Messages;

    class EndpointCreator
    {
        EndpointCreator(SettingsHolder settings, HostingComponent.Configuration hostingConfiguration, Conventions conventions)
        {
            this.settings = settings;
            this.hostingConfiguration = hostingConfiguration;
            this.conventions = conventions;
        }

        public static EndpointCreator Create(SettingsHolder settings, HostingComponent.Configuration hostingConfiguration)
        {
            var endpointCreator = new EndpointCreator(settings, hostingConfiguration, settings.Get<Conventions>());

            endpointCreator.Initialize();

            return endpointCreator;
        }

        void Initialize()
        {
            ConfigureMessageTypes();

            var pipelineSettings = settings.Get<PipelineSettings>();

            hostingConfiguration.Container.RegisterSingleton<ReadOnlySettings>(settings);

            featureComponent = new FeatureComponent(settings);

            // This needs to happen here to make sure that features enabled state is present in settings so both
            // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
            featureComponent.RegisterFeatureEnabledStatusInSettings(hostingConfiguration);

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

            recoverabilityComponent.Initialize(receiveConfiguration, hostingConfiguration, transportSeam);

            var routingComponent = RoutingComponent.Initialize(
                routingConfiguration,
                transportSeam,
                receiveConfiguration,
                settings.Get<Conventions>(),
                pipelineSettings);

            sendComponent = SendComponent.Initialize(pipelineSettings, hostingConfiguration, routingComponent, messageMapper, transportSeam);

            hostingConfiguration.Container.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            receiveComponent = ReceiveComponent.Initialize(
                receiveConfiguration,
                settings.ErrorQueueAddress(),
                hostingConfiguration,
                pipelineSettings);

            pipelineComponent = PipelineComponent.Initialize(pipelineSettings, hostingConfiguration);

            // The settings can only be locked after initializing the feature component since it uses the settings to store & share feature state.
            // As well as all the other components have been initialized
            settings.PreventChanges();

            // The pipeline settings can be locked after the endpoint is configured. It prevents end users from modyfing pipeline after an endpoint has started.
            pipelineSettings.PreventChanges();

            settings.AddStartupDiagnosticsSection("Endpoint",
                new
                {
                    Name = settings.EndpointName(),
                    SendOnly = settings.Get<bool>("Endpoint.SendOnly"),
                    NServiceBusVersion = GitVersionInformation.MajorMinorPatch
                }
            );
        }

        void ConfigureMessageTypes()
        {
            var messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType);

            messageMetadataRegistry.RegisterMessageTypesFoundIn(settings.GetAvailableTypes());

            settings.Set(messageMetadataRegistry);

            var foundMessages = messageMetadataRegistry.GetAllMessages().ToList();

            settings.AddStartupDiagnosticsSection("Messages", new
            {
                CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
                MessageConventions = conventions.RegisteredConventions,
                NumberOfMessagesFoundAtStartup = foundMessages.Count,
                Messages = foundMessages.Select(m => m.MessageType.FullName)
            });
        }

        public IStartableEndpoint CreateStartableEndpoint(IServiceProvider builder, HostingComponent hostingComponent)
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

        PipelineComponent pipelineComponent;
        FeatureComponent featureComponent;
        ReceiveComponent receiveComponent;
        RecoverabilityComponent recoverabilityComponent;
        SendComponent sendComponent;
        TransportSeam transportSeam;

        readonly SettingsHolder settings;
        readonly HostingComponent.Configuration hostingConfiguration;
        readonly Conventions conventions;
    }
}
