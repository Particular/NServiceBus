namespace NServiceBus.Unicast
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(IBuilder builder, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner, IBusContextFactory busContextFactory)
        {
            this.busContextFactory = busContextFactory;
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
                var busContext = CreateBusContext();
                await featureRunner.Stop(busContext).ConfigureAwait(false);
                await startAndStoppablesRunner.Stop(busContext).ConfigureAwait(false);
                builder.Dispose();

                stopped = true;
                Log.Info("Shutdown complete.");
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public IBusContext CreateBusContext()
        {
            return busContextFactory.CreateBusContext();
        }

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBuilder builder;
        IBusContextFactory busContextFactory;

        static ILog Log = LogManager.GetLogger<UnicastBus>();
    }
}