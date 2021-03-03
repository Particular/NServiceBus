namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;

    /// <summary>
    /// Provides access to receive completed notifications.
    /// </summary>
    public static class ReceiveConfigExtensions
    {
        /// <summary>
        /// Subscribes to notifications for receive completed events.
        /// </summary>
        public static void OnReceiveCompleted(this FeatureConfigurationContext featureConfigurationContext, Func<ReceiveCompleted, CancellationToken, Task> subscription)
        {
            Guard.AgainstNull(nameof(featureConfigurationContext), featureConfigurationContext);
            Guard.AgainstNull(nameof(subscription), subscription);

            featureConfigurationContext.Settings.Get<ReceiveComponent.Settings>().ReceiveCompletedSubscribers.Subscribe(subscription);
        }
    }
}