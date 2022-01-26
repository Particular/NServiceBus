namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Features;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Microsoft.Extensions.DependencyInjection;
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

            endpointCreator.Configure();

            return endpointCreator;
        }

        void Configure()
        {
            ConfigureMessageTypes();

            var pipelineSettings = settings.Get<PipelineSettings>();

            hostingConfiguration.Services.AddSingleton(typeof(IReadOnlySettings), settings);

            featureComponent = new FeatureComponent(settings);

            // This needs to happen here to make sure that features enabled state is present in settings so both
            // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
            featureComponent.RegisterFeatureEnabledStatusInSettings(hostingConfiguration);

            transportSeam = TransportSeam.Create(settings.Get<TransportSeam.Settings>(), hostingConfiguration);

            var receiveConfiguration = ReceiveComponent.PrepareConfiguration(
                hostingConfiguration,
                settings.Get<ReceiveComponent.Settings>(),
                transportSeam);

            var routingConfiguration = RoutingComponent.Configure(settings.Get<RoutingComponent.Settings>());

            var messageMapper = new MessageMapper();
            settings.Set<IMessageMapper>(messageMapper);

            recoverabilityComponent = new RecoverabilityComponent(settings);

            var featureConfigurationContext = new FeatureConfigurationContext(settings, hostingConfiguration.Services, pipelineSettings, routingConfiguration, receiveConfiguration);

            featureComponent.Initalize(featureConfigurationContext);

            recoverabilityComponent.Initialize(receiveConfiguration, hostingConfiguration, transportSeam);

            var routingComponent = RoutingComponent.Initialize(
                routingConfiguration,
                receiveConfiguration,
                settings.Get<Conventions>(),
                pipelineSettings,
                hostingConfiguration,
                transportSeam);

            sendComponent = SendComponent.Initialize(pipelineSettings, hostingConfiguration, routingComponent, messageMapper);

            hostingConfiguration.Services.ConfigureComponent(b => settings.Get<Notifications>(), DependencyLifecycle.SingleInstance);

            receiveComponent = ReceiveComponent.Configure(
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
            IList<Type> availableTypes = settings.GetAvailableTypes();
            //TODO we also could that lazily as part of the resolving strategy
            conventions.Add(new UnobtrusiveConventions(availableTypes));

            var messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType);

            messageMetadataRegistry.RegisterMessageTypesFoundIn(availableTypes);

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
                transportSeam,
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

    class UnobtrusiveConventions : IMessageConvention
    {
        List<Func<Type, bool>> messageConventions;
        List<Func<Type, bool>> commandConventions;
        List<Func<Type, bool>> eventConventions;

        public UnobtrusiveConventions(IList<Type> availableTypes)
        {
            var conventionTypes = availableTypes.Where(t => t.Name.EndsWith("Convention")).ToList();
            messageConventions = conventionTypes
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.Name.Equals("IsMessageType") && f.FieldType == typeof(Func<Type, bool>)))
                .Select(f => f.GetValue(f.DeclaringType) as Func<Type, bool>)
                .ToList();
            commandConventions = conventionTypes
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.Name.Equals("IsCommandType") && f.FieldType == typeof(Func<Type, bool>)))
                .Select(f => f.GetValue(f.DeclaringType) as Func<Type, bool>)
                .ToList();
            eventConventions = conventionTypes
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.Name.Equals("IsEventType") && f.FieldType == typeof(Func<Type, bool>)))
                .Select(f => f.GetValue(f.DeclaringType) as Func<Type, bool>)
                .ToList();
        }

        public string Name { get; }
        public bool IsMessageType(Type type) => messageConventions.Any(c => c(type));

        public bool IsCommandType(Type type) => commandConventions.Any(c => c(type));

        public bool IsEventType(Type type) => eventConventions.Any(c => c(type));
    }
}
