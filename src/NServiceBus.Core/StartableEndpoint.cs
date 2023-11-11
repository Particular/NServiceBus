namespace NServiceBus;

using System;
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
    }

    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

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

    MessageSession messageSession;
    TransportInfrastructure transportInfrastructure;
    CancellationTokenSource stoppingTokenSource;
}