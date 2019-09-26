namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings, ContainerComponent containerComponent, FeatureComponent featureComponent, TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CriticalError criticalError, PipelineComponent pipelineComponent, RecoverabilityComponent recoverabilityComponent)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.featureComponent = featureComponent;
            this.transportInfrastructure = transportInfrastructure;
            this.receiveComponent = receiveComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.pipelineComponent = pipelineComponent;
        }

        public async Task<IEndpointInstance> Start()
        {
            var builder = containerComponent.Builder;
            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(builder));

            await receiveComponent.PerformPreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.Initialize(containerComponent, recoverabilityComponent).ConfigureAwait(false);

            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, containerComponent, receiveComponent, featureComponent, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            receiveComponent.Start();

            return runningInstance;
        }

        RecoverabilityComponent recoverabilityComponent;
        PipelineComponent pipelineComponent;
        ContainerComponent containerComponent;
        FeatureComponent featureComponent;
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        ReceiveComponent receiveComponent;
        CriticalError criticalError;
    }
}