namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    partial class UnicastBusInternal : IStoppableEndpoint
    {
        public UnicastBusInternal(IBuilder builder, ReadOnlySettings settings, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner)
        {
            this.pipelineCollection = pipelineCollection;
            this.startAndStoppablesRunner = startAndStoppablesRunner;
            this.featureRunner = featureRunner;
            this.builder = builder;
            this.settings = settings;
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

        public IBus CreateOutgoingContext()
        {
            return new ContextualBus(new BehaviorContextStacker(builder), builder.Build<IMessageMapper>(), builder, settings);
        }

        volatile bool stopped;

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBuilder builder;
        readonly ReadOnlySettings settings;
    }
}