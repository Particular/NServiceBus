namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Settings;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings,
            ContainerComponent containerComponent,
            FeatureComponent featureComponent,
            TransportComponent transportComponent,
            ReceiveComponent receiveComponent,
            CriticalError criticalError,
            PipelineComponent pipelineComponent,
            RecoverabilityComponent recoverabilityComponent,
            HostingComponent hostingComponent)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.featureComponent = featureComponent;
            this.transportComponent = transportComponent;
            this.receiveComponent = receiveComponent;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.hostingComponent = hostingComponent;
        }

        public async Task<IEndpointInstance> Start()
        {
            await pipelineComponent.Start().ConfigureAwait(false);

            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(containerComponent.Builder));

            await receiveComponent.PerformPreStartupChecks().ConfigureAwait(false);

            await transportComponent.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.PrepareToStart(containerComponent, recoverabilityComponent).ConfigureAwait(false);

            await featureComponent.Start(messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, containerComponent, receiveComponent, featureComponent, messageSession, transportComponent);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            await receiveComponent.Start().ConfigureAwait(false);

            await hostingComponent.Start().ConfigureAwait(false);

            return runningInstance;
        }

        PipelineComponent pipelineComponent;
        RecoverabilityComponent recoverabilityComponent;
        HostingComponent hostingComponent;
        ContainerComponent containerComponent;
        FeatureComponent featureComponent;
        SettingsHolder settings;
        TransportComponent transportComponent;
        ReceiveComponent receiveComponent;
        CriticalError criticalError;
    }
}