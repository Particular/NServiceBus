namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        await GenerateManifest(transportInfrastructure, receiveComponent, cancellationToken).ConfigureAwait(false);
    }

    Task GenerateManifest(TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CancellationToken cancellationToken = default)
    {
        try
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

            var conventions = settings.Get<Conventions>();
            var receiveManifest = receiveComponent.GetManifest(conventions);
            var events = receiveManifest.HandledMessages
                .Where(handledMessage => handledMessage.IsEvent && !handledMessage.IsCommand && !conventions.IsInSystemConventionList(handledMessage.MessageType))
                .Select(handledMessage => handledMessage.MessageType.FullName)
                .ToArray();
            var transportManifest = transportInfrastructure.GetManifest(events);

            manifest = new ManifestItem
            {
                ItemValue = [
                    .. BaseManifestItems(),
                .. transportManifest,
                .. receiveManifest.ToMessageManifest(),
                .. persistenceManifest
                ]
            };
        }
        catch (Exception)
        {
            Debug.WriteLine("Generating the manifest failed. This is non-critical and the endpoint will continue to start.");
        }

        return Task.CompletedTask;
    }

    IEnumerable<KeyValuePair<string, ManifestItem>> BaseManifestItems()
    {
        yield return new("endpointName", new ManifestItem { StringValue = settings.EndpointName() });
        if (settings.TryGet("EndpointInstanceDiscriminator", out string discriminator))
        {
            yield return new("uniqueAddressDiscriminator", new ManifestItem { StringValue = discriminator });
        }
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