namespace NServiceBus.Pipeline
{
    using System;

    interface IExecutor : IDisposable
    {
        void Start(string[] pipelineIds);
        void Execute(string pipelineId, Action action);
        void Stop();
    }
}