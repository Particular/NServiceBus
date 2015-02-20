namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class IndividualThroughputLimitExecutor : IExecutor
    {
        readonly int? defaultLimit;
        readonly Dictionary<string, int?> limits;
        Dictionary<string, IExecutor> limiters; 

        public IndividualThroughputLimitExecutor(int? defaultLimit, Dictionary<string, int?> limits)
        {
            this.defaultLimit = defaultLimit;
            this.limits = limits;
        }

        static IExecutor CreateIndividualExecutor(int? limit)
        {
            return limit.HasValue
                ? (IExecutor) new ThroughputLimitExecutor(limit.Value)
                : new SynchronousExecutor();
        }

        public virtual void Dispose()
        {
            //Generated
        }

        void DisposeManaged()
        {
            foreach (var limiter in limiters)
            {
                limiter.Value.Dispose();
            }
        }

        public virtual void Start(string[] pipelineIds)
        {
            if (limiters != null)
            {
                throw new InvalidOperationException("Executor already started");
            }
            limiters = pipelineIds.ToDictionary(x => x, x =>
            {
                int? overridden;
                return CreateIndividualExecutor(limits.TryGetValue(x, out overridden) 
                    ? overridden 
                    : defaultLimit);
            });
            foreach (var limiter in limiters)
            {
                limiter.Value.Start(pipelineIds);
            }
        }

        public virtual void Execute(string pipelineId, Action action)
        {
            limiters[pipelineId].Execute(pipelineId, action);
        }

        public virtual void Stop()
        {
            foreach (var limiter in limiters)
            {
                limiter.Value.Stop();
            }
        }
    }
}