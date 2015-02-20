namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Internal NSB executor
    /// </summary>
    public interface IExecutor : IDisposable
    {
        /// <summary>
        /// </summary>
        void Start(string[] pipelineIds);
        /// <summary>
        /// </summary>
        void Execute(string pipelineId, Action action);
        /// <summary>
        /// </summary>
        void Stop();
    }
}