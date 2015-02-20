namespace NServiceBus.Pipeline
{
    using System;

    class SynchronousExecutor : IExecutor
    {
        public virtual void Dispose()
        {
        }

        public virtual void Start(string[] pipelineIds)
        {
        }

        public virtual void Execute(string pipelineId, Action action)
        {
            action();
        }

        public virtual void Stop()
        {
        }
    }
}