// ReSharper disable ConvertToAutoProperty
// we need writable fields for disposing
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
        ///     Errors push-based notifications.
        /// </summary>
        public ErrorsNotifications Errors => errorNotifications;

        ErrorsNotifications errorNotifications = new ErrorsNotifications();

        /// <summary>
        ///     Pipeline push-based notifications.
        /// </summary>
        public PipelineNotifications Pipeline => pipeNotifications;

        PipelineNotifications pipeNotifications = new PipelineNotifications();


        void IDisposable.Dispose()
        {
            // Injected
        }
    }
}