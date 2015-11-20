// ReSharper disable ConvertToAutoProperty
// we need writable fields for disposing
namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;

    /// <summary>
    ///     Bus notifications.
    /// </summary>
    public partial class BusNotifications: IDisposable
    {
        /// <summary>
        ///     Errors push-based notifications.
        /// </summary>
        public ErrorsNotifications Errors => errorNotifications;

        ErrorsNotifications errorNotifications = new ErrorsNotifications();

        void IDisposable.Dispose()
        {
            // Injected
        }
    }
}