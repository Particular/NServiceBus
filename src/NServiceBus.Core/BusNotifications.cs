// ReSharper disable ConvertToAutoProperty
// we need writable fields for disposing
namespace NServiceBus
{
    using NServiceBus.Faults;

    /// <summary>
    ///     Bus notifications.
    /// </summary>
    public partial class BusNotifications
    {
        /// <summary>
        ///     Errors push-based notifications.
        /// </summary>
        public ErrorsNotifications Errors => errorNotifications;

        ErrorsNotifications errorNotifications = new ErrorsNotifications();
    }
}