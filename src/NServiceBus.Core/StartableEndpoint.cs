namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings,
            FeatureComponent featureComponent,
            ReceiveComponent receiveComponent,
            TransportInfrastructure transportInfrastructure,
            PipelineComponent pipelineComponent,
            RecoverabilityComponent recoverabilityComponent,
            HostingComponent hostingComponent,
            SendComponent sendComponent,
            IBuilder builder)
        {
            this.settings = settings;
            this.featureComponent = featureComponent;
            this.receiveComponent = receiveComponent;
            this.transportInfrastructure = transportInfrastructure;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.hostingComponent = hostingComponent;
            this.sendComponent = sendComponent;
            this.builder = builder;
        }

        public async Task<IEndpointInstance> Start()
        {
            await receiveComponent.ReceivePreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);
            // This is a hack to maintain the current order of transport infrastructure initialization
            sendComponent.ConfigureSendInfrastructureForBackwardsCompatibility();

            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var rootContext = new RootContext(builder, messageOperations, pipelineCache);
            var messageSession = new MessageSession(rootContext);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.PrepareToStart(builder, recoverabilityComponent, messageOperations, pipelineCache).ConfigureAwait(false);

            // This is a hack to maintain the current order of transport infrastructure initialization
            await sendComponent.InvokeSendPreStartupChecksForBackwardsCompatibility().ConfigureAwait(false);

            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, hostingComponent, receiveComponent, featureComponent, messageSession, transportInfrastructure);

            await receiveComponent.Start().ConfigureAwait(false);

            await hostingComponent.Start(runningInstance).ConfigureAwait(false);

            return runningInstance;
        }

        readonly PipelineComponent pipelineComponent;
        readonly RecoverabilityComponent recoverabilityComponent;
        readonly HostingComponent hostingComponent;
        readonly SendComponent sendComponent;
        readonly IBuilder builder;
        readonly FeatureComponent featureComponent;
        readonly SettingsHolder settings;
        readonly ReceiveComponent receiveComponent;
        readonly TransportInfrastructure transportInfrastructure;
    }
}