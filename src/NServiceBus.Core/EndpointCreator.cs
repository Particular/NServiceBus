namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        var assemblyScanningConfiguration = settings.Get<AssemblyScanningComponent.Configuration>();
        var assemblyScanningComponent = AssemblyScanningComponent.Initialize(assemblyScanningConfiguration, settings);

        assemblyScanningConfiguration.SetDefaultAvailableTypes(assemblyScanningComponent.AvailableTypes);

        endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

        var persistenceSettings = settings.GetOrCreate<PersistenceComponent.Settings>();
        var persistenceComponent = new PersistenceComponent(persistenceSettings);

        var persistenceConfiguration = persistenceComponent.Initialize(settings);

        var availableTypes = assemblyScanningComponent.AvailableTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

        var installerSettings = settings.Get<InstallerComponent.Settings>();

        DiscoverInstallers(installerSettings, availableTypes);

        var installerComponent = new InstallerComponent(installerSettings);

        installerComponent.Initialize(settings);

        var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), availableTypes, persistenceConfiguration, installerComponent, serviceCollection);

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

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingSuppressJustification)]
        static void DiscoverInstallers(InstallerComponent.Settings installerSettings, List<Type> availableTypes) => installerSettings.AddScannedInstallers(availableTypes);
    }

    void Configure()
    {
        var receiveSettings = settings.Get<ReceiveComponent.Settings>();

        DiscoverHandlers(receiveSettings, hostingConfiguration.AvailableTypes);

        ConfigureMessageTypes(receiveSettings.MessageHandlerRegistry.GetMessageTypes());

        var pipelineSettings = settings.Get<PipelineSettings>();

        hostingConfiguration.Services.AddSingleton<IReadOnlySettings>(settings);

        var featureSettings = settings.Get<FeatureComponent.Settings>();

        // This needs to happen here to make sure that features enabled state is present in settings so both
        // IWantToRunBeforeConfigurationIsFinalized implementations and transports can check access it
        featureSettings.AddScannedTypes(hostingConfiguration.AvailableTypes);

        transportSeam = TransportSeam.Create(settings.Get<TransportSeam.Settings>(), hostingConfiguration);

        var receiveConfiguration = ReceiveComponent.PrepareConfiguration(
            settings.Get<ReceiveComponent.Settings>(),
            transportSeam);

        var routingConfiguration = RoutingComponent.Configure(settings.Get<RoutingComponent.Settings>());

        var messageMapper = new MessageMapper();
        settings.Set<IMessageMapper>(messageMapper);

        recoverabilityComponent = new RecoverabilityComponent(settings);

        var sagaSettings = settings.Get<SagaComponent.Settings>();

        sagaSettings.AddDiscoveredSagas(hostingConfiguration.AvailableTypes);

        SagaComponent.Configure(sagaSettings, hostingConfiguration.PersistenceConfiguration);

        featureComponent = new FeatureComponent(featureSettings);
        var featureConfigurationContext = new FeatureConfigurationContext(settings, hostingConfiguration.Services, pipelineSettings, routingConfiguration, receiveConfiguration, hostingConfiguration.PersistenceConfiguration);
        featureComponent.Initialize(featureConfigurationContext, settings);

        hostingConfiguration.PersistenceConfiguration.AssertSagaAndOutboxUseSamePersistence();

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
            hostingConfiguration);

        sendComponent = SendComponent.Initialize(pipelineSettings, hostingConfiguration, routingComponent, messageMapper);

        envelopeComponent = new EnvelopeComponent(settings.Get<EnvelopeComponent.Settings>());

        receiveComponent = ReceiveComponent.Configure(
            receiveConfiguration,
            settings.ErrorQueueAddress(),
            hostingConfiguration,
            pipelineSettings);
        receiveComponent.AddManifest(hostingConfiguration, settings);

        pipelineComponent = PipelineComponent.Initialize(pipelineSettings, hostingConfiguration, receiveConfiguration);

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

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingSuppressJustification)]
        static void DiscoverHandlers(ReceiveComponent.Settings receiveSettings, ICollection<Type> availableTypes) => receiveSettings.MessageHandlerRegistry.AddScannedHandlers(availableTypes);
    }

    void ConfigureMessageTypes(IEnumerable<Type> messageTypesHandled)
    {
        var allowDynamicTypeLoading = settings.IsDynamicTypeLoadingEnabled();
        var messageMetadataRegistry = settings.GetOrCreate<MessageMetadataRegistry>();
        messageMetadataRegistry.Initialize(conventions.IsMessageType, allowDynamicTypeLoading);

        messageMetadataRegistry.RegisterMessageTypes(hostingConfiguration.AvailableTypes);
        messageMetadataRegistry.RegisterMessageTypesBypassingChecks(messageTypesHandled);

        var foundMessages = messageMetadataRegistry.GetAllMessages();

        settings.AddStartupDiagnosticsSection("Messages", new
        {
            CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
            MessageConventions = conventions.RegisteredConventions,
            NumberOfMessagesFoundAtStartup = foundMessages.Length,
            Messages = foundMessages.Select(m => m.MessageType.FullName),
            AllowDynamicTypeLoading = allowDynamicTypeLoading
        });
    }

    public StartableEndpoint CreateStartableEndpoint(IServiceProvider serviceProvider, bool serviceProviderIsExternallyManaged)
    {
        hostingConfiguration.AddStartupDiagnosticsSection("Container", new { Type = serviceProviderIsExternallyManaged ? "external" : "internal" });

        return new StartableEndpoint(settings,
            featureComponent,
            envelopeComponent,
            receiveComponent,
            transportSeam,
            pipelineComponent,
            recoverabilityComponent,
            hostingComponent,
            sendComponent,
            serviceProvider,
            serviceProviderIsExternallyManaged);
    }

    PipelineComponent pipelineComponent;
    FeatureComponent featureComponent;
    ReceiveComponent receiveComponent;
    RecoverabilityComponent recoverabilityComponent;
    SendComponent sendComponent;
    TransportSeam transportSeam;
    HostingComponent hostingComponent;
    EnvelopeComponent envelopeComponent;

    readonly SettingsHolder settings;
    readonly HostingComponent.Configuration hostingConfiguration;
    readonly Conventions conventions;

#pragma warning disable IDE0051
    const string TrimmingSuppressJustification = "The assembly scanning component has a guard that prevents it from being used when dynamic code is not available so we can safely call this.";
#pragma warning restore IDE0051
}