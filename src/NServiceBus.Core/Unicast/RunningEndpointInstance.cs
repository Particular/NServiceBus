namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using UnicastBus = NServiceBus.Unicast.UnicastBus;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(IChildBuilder builder, PipelineCollection pipelineCollection, StartAndStoppablesRunner startAndStoppablesRunner, FeatureRunner featureRunner, IBusSession busSession)
        {
            this.builder = builder;
            this.pipelineCollection = pipelineCollection;
            this.startAndStoppablesRunner = startAndStoppablesRunner;
            this.featureRunner = featureRunner;
            this.busSession = busSession;
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

        public Task Send(object message, SendOptions options)
        {
            return busSession.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return busSession.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return busSession.Publish(message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return busSession.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return busSession.Subscribe(eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return busSession.Unsubscribe(eventType, options);
        }

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        PipelineCollection pipelineCollection;
        StartAndStoppablesRunner startAndStoppablesRunner;
        FeatureRunner featureRunner;
        IBusSession busSession;
        IChildBuilder builder;

        static ILog Log = LogManager.GetLogger<UnicastBus>();
    }
}