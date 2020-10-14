namespace NServiceBus
{
    using System;
#if NETSTANDARD
    using System.Runtime.InteropServices;
#endif
    using System.Security.Principal;
    using System.Threading.Tasks;
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
            IServiceProvider builder)
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

            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var rootContext = new RootContext(builder, messageOperations, pipelineCache);
            var messageSession = new MessageSession(rootContext);

#if NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                 AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            }
#else
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
#endif
            await featureComponent.Start(builder, messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, hostingComponent, receiveComponent, featureComponent, messageSession, transportInfrastructure);

            await receiveComponent.Start(builder, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache).ConfigureAwait(false);

            return runningInstance;
        }

        readonly PipelineComponent pipelineComponent;
        readonly RecoverabilityComponent recoverabilityComponent;
        readonly HostingComponent hostingComponent;
        readonly SendComponent sendComponent;
        readonly IServiceProvider builder;
        readonly FeatureComponent featureComponent;
        readonly SettingsHolder settings;
        readonly ReceiveComponent receiveComponent;
        readonly TransportInfrastructure transportInfrastructure;
    }
}