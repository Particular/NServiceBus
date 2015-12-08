namespace NServiceBus
{
    using NServiceBus.Faults;

    /// <summary>
    /// The signature of the delegate used by <see cref="NotificationExtensions.NotifyOnFailedMessage"/>.
    /// </summary>
    /// <param name="failedMessage">The context of the failed message.</param>
    public delegate void FailedMessageAction(FailedMessage failedMessage);
}