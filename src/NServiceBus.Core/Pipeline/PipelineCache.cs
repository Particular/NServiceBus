namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class PipelineCache : IPipelineCache
    {
        Dictionary<Type, Lazy<IPipeline>> pipelines = new Dictionary<Type, Lazy<IPipeline>>();

        public PipelineCache(IBuilder builder, ReadOnlySettings settings)
        {
            FromMainPipeline<IAuditContext>(builder, settings);
            FromMainPipeline<IOutgoingPublishContext>(builder, settings);
            FromMainPipeline<ISubscribeContext>(builder, settings);
            FromMainPipeline<IUnsubscribeContext>(builder, settings);
            FromMainPipeline<IOutgoingSendContext>(builder, settings);
            FromMainPipeline<IOutgoingReplyContext>(builder, settings);
            FromMainPipeline<IRoutingContext>(builder, settings);
            FromMainPipeline<IBatchDispatchContext>(builder, settings);
            FromMainPipeline<IForwardingContext>(builder, settings);
            FromMainPipeline<IFaultContext>(builder, settings);
        }

        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext

        {
            Lazy<IPipeline> lazyPipeline;
            if (pipelines.TryGetValue(typeof(TContext), out lazyPipeline))
            {
                return (IPipeline<TContext>)lazyPipeline.Value;
            }
            return default(IPipeline<TContext>);
        }

        void FromMainPipeline<TContext>(IBuilder builder, ReadOnlySettings settings)
            where TContext : IBehaviorContext
        {
            var lazyPipeline = new Lazy<IPipeline>(() =>
            {
                var pipelinesCollection = settings.Get<PipelineConfiguration>();
                var pipeline = new Pipeline<TContext>(builder, settings, pipelinesCollection.MainPipeline);
                return pipeline;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            pipelines.Add(typeof(TContext), lazyPipeline);
        }
    }
}