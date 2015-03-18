namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.Unicast.Transport;

    class ThroughputLimitExecutor : IExecutor
    {
        readonly int limit;
        ThroughputLimiter limiter;

        public ThroughputLimitExecutor(int limit)
        {
            this.limit = limit;
            limiter = new ThroughputLimiter();
        }

        public virtual void Start(string[] pipelineIds)
        {
            limiter.Start(limit);
        }

        public virtual void Execute(string pipelineId, Action action)
        {
            action();
            limiter.MessageProcessed();
        }

        public virtual void Stop()
        {
            limiter.Stop();
        }

        public virtual void Dispose()
        {
            //Generated
        }
    }
}