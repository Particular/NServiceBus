namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Pipeline;

    class PipelineCache : IPipelineCache
    {
        public PipelineCache(IServiceProvider rootBuilder, PipelineModifications pipelineModifications)
        {
            this.pipelineModifications = pipelineModifications;

            FromMainPipeline<IAuditContext>(rootBuilder);
            FromMainPipeline<IDispatchContext>(rootBuilder);
            FromMainPipeline<IRoutingContext>(rootBuilder);
            FromMainPipeline<IBatchDispatchContext>(rootBuilder);
            FromMainPipeline<IForwardingContext>(rootBuilder);
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

        void FromMainPipeline<TContext>(IServiceProvider builder)
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