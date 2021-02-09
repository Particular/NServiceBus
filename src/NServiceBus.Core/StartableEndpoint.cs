namespace NServiceBus
{
    using System;
#if NETSTANDARD
    using System.Runtime.InteropServices;
#endif
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Settings;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings,
            FeatureComponent featureComponent,
            ReceiveComponent receiveComponent,
            TransportSeam transportSeam,
            PipelineComponent pipelineComponent,
            RecoverabilityComponent recoverabilityComponent,
            HostingComponent hostingComponent,
            SendComponent sendComponent,
            IServiceProvider builder)
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
        }

        public async Task<IEndpointInstance> Start()
        {
            var transportInfrastructure = await transportSeam.CreateTransportInfrastructure().ConfigureAwait(false);

            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            var messageOperations = sendComponent.CreateMessageOperations(builder, pipelineComponent);
            var rootContext = new RootContext(builder, messageOperations, pipelineCache);
            var messageSession = new MessageSession(rootContext);

            await receiveComponent.Initialize(builder, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure).ConfigureAwait(false);

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

            await receiveComponent.Start().ConfigureAwait(false);

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
        readonly TransportSeam transportSeam;
    }
}