namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings, IBuilder builder, FeatureActivator featureActivator, PipelineConfiguration pipelineConfiguration, IEventAggregator eventAggregator, TransportInfrastructure transportInfrastructure, ReceiveComponent receiving, CriticalError criticalError)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.builder = builder;
            this.featureActivator = featureActivator;
            this.pipelineConfiguration = pipelineConfiguration;
            this.eventAggregator = eventAggregator;
            this.transportInfrastructure = transportInfrastructure;
            this.receiving = receiving;

            pipelineCache = new PipelineCache(builder, settings);

            messageSession = new MessageSession(new RootContext(builder, pipelineCache, eventAggregator));
        }

        public async Task<IEndpointInstance> Start()
        {
            await receiving.PerformPreStartupChecks().ConfigureAwait(false);

            await transportInfrastructure.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            var mainPipeline = new Pipeline<ITransportReceiveContext>(builder, pipelineConfiguration.Modifications);
            var mainPipelineExecutor = new MainPipelineExecutor(builder, eventAggregator, pipelineCache, mainPipeline);

            await receiving.InitializeReceivers(mainPipelineExecutor, eventAggregator, builder, criticalError).ConfigureAwait(false);

            var featureRunner = await StartFeatures(messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, builder, receiving, featureRunner, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            receiving.Start();

            return runningInstance;
        }

        async Task<FeatureRunner> StartFeatures(IMessageSession session)
        {
            var featureRunner = new FeatureRunner(featureActivator);
            await featureRunner.Start(builder, session).ConfigureAwait(false);
            return featureRunner;
        }

        IMessageSession messageSession;
        IBuilder builder;
        FeatureActivator featureActivator;

        IPipelineCache pipelineCache;
        PipelineConfiguration pipelineConfiguration;

        SettingsHolder settings;
        IEventAggregator eventAggregator;
        TransportInfrastructure transportInfrastructure;
        ReceiveComponent receiving;
        CriticalError criticalError;
    }
}