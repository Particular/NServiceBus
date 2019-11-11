namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Pipeline.Outgoing;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings, ContainerComponent containerComponent, FeatureComponent featureComponent, TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CriticalError criticalError, PipelineComponent pipelineComponent, RecoverabilityComponent recoverabilityComponent, SendComponent sendComponent)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.featureComponent = featureComponent;
            this.transportInfrastructure = transportInfrastructure;
            this.receiveComponent = receiveComponent;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.sendComponent = sendComponent;
        }

        public async Task<IEndpointInstance> Start()
        {
            await pipelineComponent.Start().ConfigureAwait(false);

            await sendComponent.Start(containerComponent.Builder).ConfigureAwait(false);

            await receiveComponent.PerformPreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.PrepareToStart(containerComponent, recoverabilityComponent).ConfigureAwait(false);

            await featureComponent.Start(sendComponent.MessageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, containerComponent, receiveComponent, featureComponent, sendComponent.MessageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            await receiveComponent.Start().ConfigureAwait(false);

            return runningInstance;
        }

        PipelineComponent pipelineComponent;
        RecoverabilityComponent recoverabilityComponent;
        readonly SendComponent sendComponent;
        ContainerComponent containerComponent;
        FeatureComponent featureComponent;
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        ReceiveComponent receiveComponent;
        CriticalError criticalError;
    }
}