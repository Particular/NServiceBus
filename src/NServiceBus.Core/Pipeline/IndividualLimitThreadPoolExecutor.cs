namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janitor;

    class IndividualLimitThreadPoolExecutor : IExecutor
    {
        readonly int defaultLimit;
        readonly Dictionary<string, int> limits;
        [SkipWeaving]
        readonly BusNotifications busNotifications;
        Dictionary<string, LimitedThreadPoolExecutor> executors;

        public IndividualLimitThreadPoolExecutor(int defaultLimit, Dictionary<string, int> limits, BusNotifications busNotifications)
        {
            this.defaultLimit = defaultLimit;
            this.limits = limits;
            this.busNotifications = busNotifications;
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
                    ? new LimitedThreadPoolExecutor(overridden, busNotifications)
                    : new LimitedThreadPoolExecutor(defaultLimit, busNotifications);
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