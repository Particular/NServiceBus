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
            var forkBehavior = new ManualRetryNotificationBehavior();
            context.Pipeline.Register(forkBehavior, "Provides retry notifications to ServiceControl");
            context.Pipeline.Register(new MarkAsAcknowledgedBehavior(), "Adds audit information about direct retry acknowledgement");
        }
    }
}