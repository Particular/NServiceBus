namespace NServiceBus
{
    using NServiceBus.Faults;

    /// <summary>
    /// The signature of the delegate used by <see cref="NotificationExtensions.NotifyOnFirstLevelRetry"/>.
    /// </summary>
    /// <param name="firstLevelRetry">The context of the first level retry.</param>
    public delegate void FirstLevelRetryAction(FirstLevelRetry firstLevelRetry);
}