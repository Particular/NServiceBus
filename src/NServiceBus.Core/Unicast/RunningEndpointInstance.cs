namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using UnicastBus = NServiceBus.Unicast.UnicastBus;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(IBuilder builder, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner, IBusSessionFactory busSessionFactory)
        {
            this.busSessionFactory = busSessionFactory;
            this.pipelineCollection = pipelineCollection;
            this.startAndStoppablesRunner = startAndStoppablesRunner;
            this.featureRunner = featureRunner;
            this.builder = builder;
        }

        public async Task Stop()
        {
            if (stopped)
            {
                return;
            }

            await stopSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (stopped)
                {
                    return;
                }

                Log.Info("Initiating shutdown.");

                await pipelineCollection.Stop().ConfigureAwait(false);
                var busSession = CreateBusSession();
                await featureRunner.Stop(busSession).ConfigureAwait(false);
                await startAndStoppablesRunner.Stop(busSession).ConfigureAwait(false);
                builder.Dispose();

                stopped = true;
                Log.Info("Shutdown complete.");
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public IBusSession CreateBusSession(bool autoDispatch = true)
        {
            return busSessionFactory.CreateBusSession(autoDispatch);
        }

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBuilder builder;
        IBusSessionFactory busSessionFactory;

        static ILog Log = LogManager.GetLogger<UnicastBus>();
    }
}