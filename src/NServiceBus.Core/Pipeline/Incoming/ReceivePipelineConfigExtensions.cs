namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    /// <summary>
    /// Provides access to receive pipeline notifications.
    /// </summary>
    public static class ReceivePipelineConfigExtensions
    {
        /// <summary>
        /// Registers a subscription for the given notification event type.
        /// </summary>
        public static void OnReceivePipelineCompleted(this PipelineSettings pipelineSettings, Func<ReceivePipelineCompleted, Task> subscription)
        {
            Guard.AgainstNull(nameof(pipelineSettings), pipelineSettings);
            Guard.AgainstNull(nameof(subscription), subscription);

            pipelineSettings.Settings.Get<NotificationSubscriptions>()
                .Subscribe(subscription);

        }
    }
}