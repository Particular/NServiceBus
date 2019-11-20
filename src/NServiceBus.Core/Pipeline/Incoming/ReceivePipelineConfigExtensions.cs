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
        /// Subscribes to notifications for completed receive pipelines.
        /// </summary>
        public static void OnReceivePipelineCompleted(this PipelineSettings pipelineSettings, Func<ReceivePipelineCompleted, Task> subscription)
        {
            Guard.AgainstNull(nameof(pipelineSettings), pipelineSettings);
            Guard.AgainstNull(nameof(subscription), subscription);

            pipelineSettings.Settings.Get<ReceiveComponent.Settings>().PipelineCompletedSubscribers.Subscribe(subscription);
        }
    }
}