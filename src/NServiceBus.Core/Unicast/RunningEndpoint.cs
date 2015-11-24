namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    partial class RunningEndpoint : IEndpoint
    {
        public RunningEndpoint(IBuilder builder, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner, IBusContextFactory busContextFactory)
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
                throw new InvalidOperationException("Endpoint already stopped.");
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
        public IBusContext CreateBusContext()
        {
            return busContextFactory.CreateBusContext();
        }

        volatile bool stopped;
        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBuilder builder;

        static ILog Log = LogManager.GetLogger<UnicastBus>();
        readonly IBusContextFactory busContextFactory;
    }
}