namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Settings;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings,
            FeatureComponent featureComponent,
            TransportComponent transportComponent,
            ReceiveComponent receiveComponent,
            PipelineComponent pipelineComponent,
            RecoverabilityComponent recoverabilityComponent,
            HostingComponent hostingComponent,
            SendComponent sendComponent,
            IBuilder builder)
        {
            this.settings = settings;
            this.featureComponent = featureComponent;
            this.transportComponent = transportComponent;
            this.receiveComponent = receiveComponent;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.hostingComponent = hostingComponent;
            this.sendComponent = sendComponent;
            this.builder = builder;
        }

        public async Task<IEndpointInstance> Start()
        {
            await pipelineComponent.Start(builder).ConfigureAwait(false);

            await transportComponent.Start().ConfigureAwait(false);
            // This is a hack to maintain the current order of transport infrastructure initialization
            transportComponent.ConfigureSendInfrastructureForBackwardsCompatibility();

            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(builder, messageOperations));

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.PrepareToStart(builder, recoverabilityComponent, messageOperations).ConfigureAwait(false);
            
            // This is a hack to maintain the current order of transport infrastructure initialization
            await transportComponent.InvokeSendPreStartupChecksForBackwardsCompatibility().ConfigureAwait(false);

            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, hostingComponent, receiveComponent, featureComponent, messageSession, transportComponent);

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
        readonly TransportComponent transportComponent;
        readonly ReceiveComponent receiveComponent;
    }
}