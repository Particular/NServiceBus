namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Features;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings, ContainerComponent containerComponent, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CriticalError criticalError, IMessageSession messageSession)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.containerComponent = containerComponent;
            this.featureActivator = featureActivator;
            this.transportInfrastructure = transportInfrastructure;
            this.receiveComponent = receiveComponent;
            this.messageSession = messageSession;
        }

        public async Task<IEndpointInstance> Start()
        {
            await receiveComponent.PerformPreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            await receiveComponent.Initialize(containerComponent).ConfigureAwait(false);

            var featureRunner = await StartFeatures().ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, containerComponent, receiveComponent, featureRunner, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            receiveComponent.Start();

            return runningInstance;
        }

        async Task<FeatureRunner> StartFeatures()
        {
            var featureRunner = new FeatureRunner(featureActivator);
            await featureRunner.Start(containerComponent.Builder, messageSession).ConfigureAwait(false);
            return featureRunner;
        }

        IMessageSession messageSession;
        ContainerComponent containerComponent;
        FeatureActivator featureActivator;
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        ReceiveComponent receiveComponent;
        CriticalError criticalError;
    }
}