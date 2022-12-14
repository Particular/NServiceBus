namespace NServiceBus
{
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
            IServiceProvider builder,
            bool shouldDisposeBuilder)
        {
            this.settings = settings;
            this.featureComponent = featureComponent;
            this.receiveComponent = receiveComponent;
            this.transportSeam = transportSeam;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.hostingComponent = hostingComponent;
            this.sendComponent = sendComponent;
            this.builder = builder;
            this.shouldDisposeBuilder = shouldDisposeBuilder;
        }

        public Task RunInstallers(CancellationToken cancellationToken = default) => hostingComponent.RunInstallers(builder, cancellationToken);

        public async Task Setup(CancellationToken cancellationToken = default)
        {
            transportInfrastructure = await transportSeam.CreateTransportInfrastructure(cancellationToken).ConfigureAwait(false);

            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            stoppingTokenSource = new CancellationTokenSource();

            messageSession = new MessageSession(builder, messageOperations, pipelineCache, stoppingTokenSource.Token);

            var consecutiveFailuresConfig = settings.Get<ConsecutiveFailuresConfiguration>();

            await receiveComponent.Initialize(builder, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure, consecutiveFailuresConfig, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
        {
            await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            }

            await featureComponent.Start(builder, messageSession, cancellationToken).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, receiveComponent, featureComponent, messageSession, transportInfrastructure, stoppingTokenSource, shouldDisposeBuilder ? builder : null);

            hostingComponent.SetupCriticalErrors(runningInstance, cancellationToken);

            await receiveComponent.Start(cancellationToken).ConfigureAwait(false);

            return runningInstance;
        }

        readonly PipelineComponent pipelineComponent;
        readonly RecoverabilityComponent recoverabilityComponent;
        readonly HostingComponent hostingComponent;
        readonly SendComponent sendComponent;
        readonly IServiceProvider builder;
        readonly bool shouldDisposeBuilder;
        readonly FeatureComponent featureComponent;
        readonly SettingsHolder settings;
        readonly ReceiveComponent receiveComponent;
        readonly TransportSeam transportSeam;

        MessageSession messageSession;
        TransportInfrastructure transportInfrastructure;
        CancellationTokenSource stoppingTokenSource;
    }
}
