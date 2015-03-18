namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Provides notficiations about state of executors.
    /// </summary>
    public class ExecutorNotifications : IDisposable
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public IObservable<ExecutorState> ExecutorState
        {
            get { return executorState; }
        }

        void IDisposable.Dispose()
        {
            // Injected
        }

        internal void ReportExecutorState(string[] pipelineIds, int currentConcurrencyLevel)
        {
            executorState.OnNext(new ExecutorState(pipelineIds, currentConcurrencyLevel));
        }

        BufferedObservable<ExecutorState> executorState = new BufferedObservable<ExecutorState>();
    }
}