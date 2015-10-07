namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;

    partial class RunningEndpoint : IEndpoint
    {
        public RunningEndpoint(IBuilder builder, ReadOnlySettings settings, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner)
        {
            this.pipelineCollection = pipelineCollection;
            this.startAndStoppablesRunner = startAndStoppablesRunner;
            this.featureRunner = featureRunner;
            this.builder = builder;
            busOperations = new BusOperations(builder.Build<IMessageMapper>(), settings);
        }

        public async Task StopAsync()
        {
            if (stopped)
            {
                throw new InvalidOperationException("Endpoint already stopped.");
            }

            Log.Info("Initiating shutdown.");

            featureRunner.Stop();
            await pipelineCollection.Stop();
            await startAndStoppablesRunner.StopAsync();
            builder.Dispose();

            stopped = true;
            Log.Info("Shutdown complete.");
        }
        public IBusContext CreateSendContext()
        {
            return new BusContext(new RootContext(builder), busOperations);
        }

        volatile bool stopped;
        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBuilder builder;
        BusOperations busOperations;

        static ILog Log = LogManager.GetLogger<UnicastBus>();
    }
}