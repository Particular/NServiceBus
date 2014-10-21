namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;

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
        ///     Errors push-based notifications
        /// </summary>
        public PipelineNotifications Pipeline
        {
            get { return pipeNotifications; }
        }

        PipelineNotifications pipeNotifications = new PipelineNotifications();

        void IDisposable.Dispose()
        {
            // Injected
        }
    }
}