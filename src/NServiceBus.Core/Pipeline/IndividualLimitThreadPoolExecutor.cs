namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class IndividualLimitThreadPoolExecutor : IExecutor
    {
        int defaultLimit;
        Dictionary<string, int> limits;
        Dictionary<string, LimitedThreadPoolExecutor> executors;

        public IndividualLimitThreadPoolExecutor(int defaultLimit, Dictionary<string, int> limits)
        {
            this.defaultLimit = defaultLimit;
            this.limits = limits;
        }

        public virtual void Dispose()
        {
            //Generated
        }

        void DisposeManaged()
        {
            foreach (var executor in executors)
            {
                executor.Value.Dispose();
            }
        }

        public virtual void Start(string[] pipelineIds)
        {
            if (executors != null)
            {
                throw new InvalidOperationException("Executor already started");
            }
            executors = pipelineIds.ToDictionary(x => x, x =>
            {
                int overridden;
                return limits.TryGetValue(x, out overridden)
                    ? new LimitedThreadPoolExecutor(overridden)
                    : new LimitedThreadPoolExecutor(defaultLimit);
            });
            foreach (var executor in executors)
            {
                executor.Value.Start(new []{executor.Key});
            }
        }

        public virtual void Execute(string pipelineId, Action action)
        {
            executors[pipelineId].Execute(pipelineId, action);
        }

        public virtual void Stop()
        {
            foreach (var executor in executors)
            {
                executor.Value.Stop();
            }
        }
    }
}