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
            ContainerComponent containerComponent,
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
            this.containerComponent = containerComponent;
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

            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(builder, messageOperations));

            await transportComponent.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.PrepareToStart(builder, recoverabilityComponent, messageOperations).ConfigureAwait(false);

            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, containerComponent, receiveComponent, featureComponent, messageSession, transportComponent);

            await receiveComponent.Start().ConfigureAwait(false);

            await hostingComponent.Start(runningInstance).ConfigureAwait(false);

            return runningInstance;
        }

        PipelineComponent pipelineComponent;
        RecoverabilityComponent recoverabilityComponent;
        HostingComponent hostingComponent;
        readonly SendComponent sendComponent;
        IBuilder builder;
        ContainerComponent containerComponent;
        FeatureComponent featureComponent;
        SettingsHolder settings;
        TransportComponent transportComponent;
        ReceiveComponent receiveComponent;
    }
}