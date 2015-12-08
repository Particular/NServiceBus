namespace NServiceBus
{
    using NServiceBus.Faults;

    /// <summary>
    /// The signature of the delegate used by <see cref="NotificationExtensions.NotifyOnSecondLevelRetry"/>.
    /// </summary>
    /// <param name="secondLevelRetry">The context of the second level retry.</param>
    public delegate void SecondLevelRetryAction(SecondLevelRetry secondLevelRetry);
}