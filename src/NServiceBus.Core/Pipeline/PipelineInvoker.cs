namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Transports;

    class PipelineInvoker : IPipelineInvoker
    {
        public PipelineInvoker(IBuilder builder, IPipelineCache pipelineCache, IPipeline<ITransportReceiveContext> pipeline, IEventAggregator eventAggregator)
        {
            this.builder = builder;
            this.pipelineCache = pipelineCache;
            this.pipeline = pipeline;
            this.eventAggregator = eventAggregator;
        }

        public async Task Invoke(PushContext pushContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = builder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, pipelineCache, eventAggregator);

                var message = new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream);
                var context = new TransportReceiveContext(message, pushContext.TransportTransaction, pushContext.ReceiveCancellationTokenSource, rootContext);

                context.Extensions.Merge(pushContext.Context);

                await pipeline.Invoke(context).ConfigureAwait(false);

                await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        IBuilder builder;
        IPipelineCache pipelineCache;
        IPipeline<ITransportReceiveContext> pipeline;
        IEventAggregator eventAggregator;
    }
}