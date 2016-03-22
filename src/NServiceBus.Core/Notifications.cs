// ReSharper disable ConvertToAutoProperty
// we need writable fields for disposing

namespace NServiceBus
{
    using Faults;

    /// <summary>
    /// Notifications.
    /// </summary>
    public partial class Notifications
    {
        /// <summary>
        /// Push-based error notifications.
        /// </summary>
        public ErrorsNotifications Errors => errorNotifications;

        ErrorsNotifications errorNotifications = new ErrorsNotifications();
    }
}