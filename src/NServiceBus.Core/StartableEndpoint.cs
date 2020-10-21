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
            TransportSeam transportSeam,
            FeatureComponent featureComponent,
            ReceiveComponent receiveComponent,
            PipelineComponent pipelineComponent,
            RecoverabilityComponent recoverabilityComponent,
            HostingComponent hostingComponent,
            SendComponent sendComponent,
            IServiceProvider builder)
        {
            this.settings = settings;
            this.transportSeam = transportSeam;
            this.featureComponent = featureComponent;
            this.receiveComponent = receiveComponent;
            this.pipelineComponent = pipelineComponent;
            this.recoverabilityComponent = recoverabilityComponent;
            this.hostingComponent = hostingComponent;
            this.sendComponent = sendComponent;
            this.builder = builder;
        }

        public async Task<IEndpointInstance> Start()
        {
            var transportInfrastructure = await transportSeam.Initialize().ConfigureAwait(false);
            var pipelineCache = pipelineComponent.BuildPipelineCache(builder);
            sendComponent.RegisterDispatcher(transportInfrastructure);
            receiveComponent.ConfigureSubscriptionManager(transportInfrastructure);
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

            var runningInstance = new RunningEndpointInstance(settings, hostingComponent, receiveComponent, featureComponent, messageSession);

            await receiveComponent.Start(builder, recoverabilityComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure).ConfigureAwait(false);

            return runningInstance;
        }

        readonly PipelineComponent pipelineComponent;
        readonly RecoverabilityComponent recoverabilityComponent;
        readonly HostingComponent hostingComponent;
        readonly SendComponent sendComponent;
        readonly IServiceProvider builder;
        readonly FeatureComponent featureComponent;
        readonly SettingsHolder settings;
        readonly TransportSeam transportSeam;
        readonly ReceiveComponent receiveComponent;
    }
}