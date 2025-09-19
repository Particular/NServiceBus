namespace NServiceBus;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
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

        AddBaseManifestItems();
    }

    void AddBaseManifestItems()
    {
        if (!hostingComponent.Config.ShouldGenerateManifest)
        {
            return;
        }

        hostingComponent.Config.AddManifestEntry("endpointName", settings.EndpointName());
        if (settings.TryGet("EndpointInstanceDiscriminator", out string discriminator))
        {
            hostingComponent.Config.AddManifestEntry("uniqueAddressDiscriminator", discriminator);
        }
        hostingComponent.Config.AddManifestEntry("sendingQueues", new ManifestItems.ManifestItem
        {
            ArrayValue = transportSeam.QueueBindings.SendingAddresses.Select(address => (ManifestItems.ManifestItem)address).ToArray()
        });
        hostingComponent.Config.AddManifestEntry("errorQueue", settings.ErrorQueueAddress());
        var auditConfig = settings.Get<AuditConfigReader.Result>();
        hostingComponent.Config.AddManifestEntry("auditEnabled", (auditConfig != null).ToString().ToLower());
        if (auditConfig != null)
        {
            hostingComponent.Config.AddManifestEntry("auditQueue", auditConfig.Address);
        }
        _ = settings.TryGet("NServiceBus.Heartbeat.Queue", out string hearbeatsQueue);
        hostingComponent.Config.AddManifestEntry("heartbeatsEnabled", (!string.IsNullOrEmpty(hearbeatsQueue)).ToString().ToLower());
        if (!string.IsNullOrEmpty(hearbeatsQueue))
        {
            hostingComponent.Config.AddManifestEntry("heartbeatsQueue", hearbeatsQueue);
        }
        _ = settings.TryGet("NServiceBus.Metrics.ServiceControl.MetricsAddress", out string metricsAddress);
        hostingComponent.Config.AddManifestEntry("monitoringEnabled", (!string.IsNullOrEmpty(metricsAddress)).ToString().ToLower());
        if (!string.IsNullOrEmpty(metricsAddress))
        {
            hostingComponent.Config.AddManifestEntry("monitoringQueue", metricsAddress);
        }
    }

    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        }

        await featureComponent.Start(serviceProvider, messageSession, cancellationToken).ConfigureAwait(false);

        await hostingComponent.WriteManifestFile(cancellationToken).ConfigureAwait(false);

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

    MessageSession messageSession;
    TransportInfrastructure transportInfrastructure;
    CancellationTokenSource stoppingTokenSource;
}