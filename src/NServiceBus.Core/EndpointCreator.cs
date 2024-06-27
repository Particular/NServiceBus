namespace NServiceBus;

using System;
using System.Linq;
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

    public static EndpointCreator Create(EndpointConfiguration endpointConfiguration, IServiceCollection serviceCollection)
    {
        var settings = endpointConfiguration.Settings;
        CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

        var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

        endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

        var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, serviceCollection);

        var endpointCreator = new EndpointCreator(settings, hostingConfiguration, settings.Get<Conventions>());
        endpointCreator.Configure();

        return endpointCreator;

        static void CheckIfSettingsWhereUsedToCreateAnotherEndpoint(SettingsHolder settings)
        {
            if (settings.GetOrDefault<bool>("UsedToCreateEndpoint"))
            {
                throw new ArgumentException("This EndpointConfiguration was already used for starting an endpoint. Each endpoint requires a new EndpointConfiguration.");
            }

            settings.Set("UsedToCreateEndpoint", true);
        }
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
            settings.Get<ReceiveComponent.Settings>(),
            transportSeam);

        var routingConfiguration = RoutingComponent.Configure(settings.Get<RoutingComponent.Settings>());

        var messageMapper = new MessageMapper();
        settings.Set<IMessageMapper>(messageMapper);

        recoverabilityComponent = new RecoverabilityComponent(settings);

        var featureConfigurationContext = new FeatureConfigurationContext(settings, hostingConfiguration.Services, pipelineSettings, routingConfiguration, receiveConfiguration);

        featureComponent.Initalize(featureConfigurationContext);

        recoverabilityComponent.Initialize(
            receiveConfiguration,
            hostingConfiguration,
            transportSeam,
            pipelineSettings);

        var routingComponent = RoutingComponent.Initialize(
            routingConfiguration,
            receiveConfiguration,
            settings.Get<Conventions>(),
            pipelineSettings,
            hostingConfiguration,
            transportSeam);

        sendComponent = SendComponent.Initialize(pipelineSettings, hostingConfiguration, routingComponent, messageMapper);

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
                NServiceBusVersion = VersionInformation.MajorMinorPatch
            }
        );

        // Make Metrics a first class citizen in Core by enabling once and for all them when creating the endpoint
        _ = hostingConfiguration.Services.AddMetrics();

        hostingComponent = HostingComponent.Initialize(hostingConfiguration);
    }


    void ConfigureMessageTypes()
    {
        var messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType, settings.IsDynamicTypeLoadingEnabled());

        messageMetadataRegistry.RegisterMessageTypesFoundIn(settings.GetAvailableTypes());

        settings.Set(messageMetadataRegistry);

        var foundMessages = messageMetadataRegistry.GetAllMessages();

        settings.AddStartupDiagnosticsSection("Messages", new
        {
            CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
            MessageConventions = conventions.RegisteredConventions,
            NumberOfMessagesFoundAtStartup = foundMessages.Length,
            Messages = foundMessages.Select(m => m.MessageType.FullName)
        });
    }

    public StartableEndpoint CreateStartableEndpoint(IServiceProvider builder, bool serviceProviderIsExternallyManaged)
    {
        hostingConfiguration.AddStartupDiagnosticsSection("Container", new
        {
            Type = serviceProviderIsExternallyManaged ? "external" : "internal"
        });

        return new StartableEndpoint(settings,
            featureComponent,
            receiveComponent,
            transportSeam,
            pipelineComponent,
            recoverabilityComponent,
            hostingComponent,
            sendComponent,
            builder,
            serviceProviderIsExternallyManaged);
    }

    PipelineComponent pipelineComponent;
    FeatureComponent featureComponent;
    ReceiveComponent receiveComponent;
    RecoverabilityComponent recoverabilityComponent;
    SendComponent sendComponent;
    TransportSeam transportSeam;
    HostingComponent hostingComponent;

    readonly SettingsHolder settings;
    readonly HostingComponent.Configuration hostingConfiguration;
    readonly Conventions conventions;
}