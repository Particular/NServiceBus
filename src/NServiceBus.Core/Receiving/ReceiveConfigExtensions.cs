namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;

    /// <summary>
    /// Provides access to processing complete notifications.
    /// </summary>
    public static class ReceiveConfigExtensions
    {
        /// <summary>
        /// Subscribes to notifications for processing completed events.
        /// </summary>
        public static void OnMessageProcessingCompleted(this FeatureConfigurationContext featureConfigurationContext, Func<ReceiveCompleted, CancellationToken, Task> subscription)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            Guard.AgainstNull(nameof(subscription), subscription);

            featureConfigurationContext.Settings.Get<ReceiveComponent.Settings>().ProcessingCompletedSubscribers.Subscribe(subscription);
        }
    }
}