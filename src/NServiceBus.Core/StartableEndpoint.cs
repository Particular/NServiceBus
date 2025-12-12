namespace NServiceBus;

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Settings;
using Transport;

class StartableEndpoint(
    SettingsHolder settings,
    FeatureComponent featureComponent,
    EnvelopeComponent envelopeComponent,
    ReceiveComponent receiveComponent,
    TransportSeam transportSeam,
    PipelineComponent pipelineComponent,
    RecoverabilityComponent recoverabilityComponent,
    HostingComponent hostingComponent,
    SendComponent sendComponent,
    IServiceProvider serviceProvider,
    bool serviceProviderIsExternallyManaged)
{
    public Task RunInstallers(CancellationToken cancellationToken = default) => hostingComponent.RunInstallers(serviceProvider, cancellationToken);

    public async Task Setup(CancellationToken cancellationToken = default)
    {
        transportInfrastructure = await transportSeam.CreateTransportInfrastructure(serviceProvider, cancellationToken).ConfigureAwait(false);

        var pipelineCache = pipelineComponent.BuildPipelineCache(serviceProvider);
        var messageOperations = sendComponent.CreateMessageOperations(serviceProvider, pipelineComponent);
        stoppingTokenSource = new CancellationTokenSource();

        messageSession = new MessageSession(serviceProvider, messageOperations, pipelineCache, stoppingTokenSource.Token);

        var consecutiveFailuresConfig = settings.Get<ConsecutiveFailuresConfiguration>();

        await receiveComponent.Initialize(serviceProvider, recoverabilityComponent, envelopeComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure, consecutiveFailuresConfig, cancellationToken).ConfigureAwait(false);

        AddSendingQueueManifest();
    }

    void AddSendingQueueManifest()
    {
        hostingComponent.Config.AddStartupDiagnosticsSection("Manifest-SendingQueues", transportSeam.QueueBindings.SendingAddresses);
        hostingComponent.Config.AddStartupDiagnosticsSection("Manifest-ErrorQueue", settings.ErrorQueueAddress());
    }

    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        }

        await featureComponent.StartFeatures(serviceProvider, messageSession, cancellationToken).ConfigureAwait(false);

        await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

        // when the service provider is externally managed it is null in the running endpoint instance
        IServiceProvider provider = serviceProviderIsExternallyManaged ? null : serviceProvider;
        var runningInstance = new RunningEndpointInstance(settings, receiveComponent, featureComponent, messageSession, transportInfrastructure, stoppingTokenSource, provider);

        hostingComponent.SetupCriticalErrors(runningInstance, cancellationToken);

        await receiveComponent.Start(cancellationToken).ConfigureAwait(false);

        return runningInstance;
    }

    MessageSession messageSession;
    TransportInfrastructure transportInfrastructure;
    CancellationTokenSource stoppingTokenSource;
}