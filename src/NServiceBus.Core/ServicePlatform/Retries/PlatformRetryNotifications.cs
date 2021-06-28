namespace NServiceBus.Features
{
    /// <summary>
    /// Provides notifications to ServiceControl about successfully retried messages.
    /// </summary>
    public class PlatformRetryNotifications : Feature
    {
        PlatformRetryNotifications() => EnableByDefault();

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueueAddress = context.Settings.ErrorQueueAddress();
            var forkBehavior = new ManualRetryNotificationBehavior(errorQueueAddress);
            context.Pipeline.Register(forkBehavior, "Provides retry notifications to ServiceControl");
        }
    }
}