namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Persistence;
using Settings;
using Transport;

class StartableEndpoint
{
    public StartableEndpoint(SettingsHolder settings,
        FeatureComponent featureComponent,
        ReceiveComponent receiveComponent,
        TransportSeam transportSeam,
        PipelineComponent pipelineComponent,
        RecoverabilityComponent recoverabilityComponent,
        HostingComponent hostingComponent,
        SendComponent sendComponent,
        IServiceProvider serviceProvider,
        bool serviceProviderIsExternallyManaged)
    {
        this.settings = settings;
        this.featureComponent = featureComponent;
        this.receiveComponent = receiveComponent;
        this.transportSeam = transportSeam;
        this.pipelineComponent = pipelineComponent;
        this.recoverabilityComponent = recoverabilityComponent;
        this.hostingComponent = hostingComponent;
        this.sendComponent = sendComponent;
        this.serviceProvider = serviceProvider;
        this.serviceProviderIsExternallyManaged = serviceProviderIsExternallyManaged;
    }

    public Task RunInstallers(CancellationToken cancellationToken = default) => hostingComponent.RunInstallers(serviceProvider, cancellationToken);

    public async Task Setup(CancellationToken cancellationToken = default)
    {
        transportInfrastructure = await transportSeam.CreateTransportInfrastructure(cancellationToken).ConfigureAwait(false);

        var pipelineCache = pipelineComponent.BuildPipelineCache(serviceProvider);
        var messageOperations = sendComponent.CreateMessageOperations(serviceProvider, pipelineComponent);
        stoppingTokenSource = new CancellationTokenSource();

        messageSession = new MessageSession(serviceProvider, messageOperations, pipelineCache, stoppingTokenSource.Token);

        var consecutiveFailuresConfig = settings.Get<ConsecutiveFailuresConfiguration>();

        await receiveComponent.Initialize(serviceProvider, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure, consecutiveFailuresConfig, cancellationToken).ConfigureAwait(false);

        await GeneratePersistenceManifest(transportInfrastructure, receiveComponent, cancellationToken).ConfigureAwait(false);
    }

    Task GeneratePersistenceManifest(TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CancellationToken cancellationToken = default)
    {
        if (!settings.TryGet("Manifest.Enable", out bool generateManifest) || !generateManifest)
        {
            return Task.CompletedTask;
        }

        var persistenceManifest = new List<KeyValuePair<string, ManifestItem>>();
        if (settings.TryGet("PersistenceDefinitions", out List<EnabledPersistence> definitions))
        {
            foreach (var definitionType in definitions)
            {
                var definition = definitionType.DefinitionType.Construct<PersistenceDefinition>();
                persistenceManifest.AddRange(definition.GetManifest(settings));
            }
        }

        var transportManifest = transportInfrastructure.GetManifest();
        var messageManifest = receiveComponent.GetManifest(settings.Get<Conventions>());

        manifest = new ManifestItem { ItemValue = [.. transportManifest, .. messageManifest, .. persistenceManifest] };

        return Task.CompletedTask;
    }


    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

        await hostingComponent.WriteManifestFile(manifest, cancellationToken).ConfigureAwait(false);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        }

        await featureComponent.Start(serviceProvider, messageSession, cancellationToken).ConfigureAwait(false);

        // when the service provider is externally managed it is null in the running endpoint instance
        IServiceProvider provider = serviceProviderIsExternallyManaged ? null : serviceProvider;
        var runningInstance = new RunningEndpointInstance(settings, receiveComponent, featureComponent, messageSession, transportInfrastructure, stoppingTokenSource, provider);

        hostingComponent.SetupCriticalErrors(runningInstance, cancellationToken);

        await receiveComponent.Start(cancellationToken).ConfigureAwait(false);

        return runningInstance;
    }

    readonly PipelineComponent pipelineComponent;
    readonly RecoverabilityComponent recoverabilityComponent;
    readonly HostingComponent hostingComponent;
    readonly SendComponent sendComponent;
    readonly IServiceProvider serviceProvider;
    readonly bool serviceProviderIsExternallyManaged;
    readonly FeatureComponent featureComponent;
    readonly SettingsHolder settings;
    readonly ReceiveComponent receiveComponent;
    readonly TransportSeam transportSeam;

    ManifestItem manifest = new ManifestItem() { StringValue = "Manifest not available" };

    MessageSession messageSession;
    TransportInfrastructure transportInfrastructure;
    CancellationTokenSource stoppingTokenSource;
}