namespace NServiceBus
{
    using System;
    using System.Runtime.InteropServices;
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
            await sendComponent.SendPreStartupChecks().ConfigureAwait(false);
            await receiveComponent.ReceivePreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);

            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var rootContext = new RootContext(builder, messageOperations, pipelineCache);
            var messageSession = new MessageSession(rootContext);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            }

            await receiveComponent.PrepareToStart(builder, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache).ConfigureAwait(false);

            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, hostingComponent, receiveComponent, featureComponent, messageSession, transportInfrastructure);

            await receiveComponent.Start().ConfigureAwait(false);

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