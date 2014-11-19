namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;

    /// <summary>
    ///     Bus notifications.
    /// </summary>
    public class BusNotifications : IDisposable
    {
        /// <summary>
        ///     Errors push-based notifications.
        /// </summary>
        public ErrorsNotifications Errors
        {
            get { return errorNotifications; }
        }

        /// <summary>
        ///     Endpoint push-based notifications.
        /// </summary>
        public EndpointNotifications Endpoint
        {
            get { return endpointNotifications; }
        }

        void IDisposable.Dispose()
        {
            // Injected
        }

        EndpointNotifications endpointNotifications = new EndpointNotifications();
        ErrorsNotifications errorNotifications = new ErrorsNotifications();
    }
}