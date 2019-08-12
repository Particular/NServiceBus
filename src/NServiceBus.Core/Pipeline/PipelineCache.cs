namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using ObjectBuilder;
    using Pipeline;

    class PipelineCache : IPipelineCache
    {
        public PipelineCache(IBuilder builder, PipelineModifications pipelineModifications)
        {
            this.pipelineModifications = pipelineModifications;

            FromMainPipeline<IAuditContext>(builder);
            FromMainPipeline<IDispatchContext>(builder);
            FromMainPipeline<IOutgoingPublishContext>(builder);
            FromMainPipeline<ISubscribeContext>(builder);
            FromMainPipeline<IUnsubscribeContext>(builder);
            FromMainPipeline<IOutgoingSendContext>(builder);
            FromMainPipeline<IOutgoingReplyContext>(builder);
            FromMainPipeline<IRoutingContext>(builder);
            FromMainPipeline<IBatchDispatchContext>(builder);
            FromMainPipeline<IForwardingContext>(builder);
            FromMainPipeline<ITransportReceiveContext>(builder);
        }

        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext
        {
            if (pipelines.TryGetValue(typeof(TContext), out var lazyPipeline))
            {
                return (IPipeline<TContext>)lazyPipeline.Value;
            }

            throw new InvalidOperationException($"Pipeline for context `{typeof(TContext).FullName}` not found, custom pipelines are not supported.");
        }

        void FromMainPipeline<TContext>(IBuilder builder)
            where TContext : IBehaviorContext
        {
            var lazyPipeline = new Lazy<IPipeline>(() =>
            {
                var pipeline = new Pipeline<TContext>(builder, pipelineModifications);
                return pipeline;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            pipelines.Add(typeof(TContext), lazyPipeline);
        }

        readonly PipelineModifications pipelineModifications;
        readonly Dictionary<Type, Lazy<IPipeline>> pipelines = new Dictionary<Type, Lazy<IPipeline>>();
    }
}