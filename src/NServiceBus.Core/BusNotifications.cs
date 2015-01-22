namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;

    /// <summary>
    ///     Bus notifications.
    /// </summary>
    public class BusNotifications: IDisposable
    {
        /// <summary>
        ///     Errors push-based notifications
        /// </summary>
        public ErrorsNotifications Errors
        {
            get { return errorNotifications; }
        }

        ErrorsNotifications errorNotifications = new ErrorsNotifications();

        /// <summary>
        ///     Pipeline push-based notifications
        /// </summary>
        public PipelineNotifications Pipeline
        {
            get { return pipeNotifications; }
        }

        PipelineNotifications pipeNotifications = new PipelineNotifications();

        /// <summary>
        /// Executor push-based notifications
        /// </summary>
        public ExecutorNotifications Executor
        {
            get { return executorNotifications; }
        }

        ExecutorNotifications executorNotifications = new ExecutorNotifications();

        void IDisposable.Dispose()
        {
            // Injected
        }
    }
}