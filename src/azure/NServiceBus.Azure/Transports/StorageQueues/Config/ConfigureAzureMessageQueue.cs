using NServiceBus.Config;
using NServiceBus.Unicast.Queuing.Azure;

namespace NServiceBus
{
    using Settings;

    public static class ConfigureAzureMessageQueue
    {
        public static Configure AzureMessageQueue(this Configure config)
        {
            return config.UseTransport<AzureStorageQueue>();
        }

        /// <summary>
        /// Sets the amount of time, in milliseconds, to add to the time to wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure PeekInterval(this Configure config, int value)
        {
            SettingsHolder.SetProperty<AzureMessageQueueReceiver>(t=>t.PeekInterval,value);

            return config;
        }

        /// <summary>
        /// Sets the maximum amount of time, in milliseconds, that the queue will wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MaximumWaitTimeWhenIdle(this Configure config, int value)
        {
            SettingsHolder.SetProperty<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, value);
         
            return config;
        }

        /// <summary>
        /// Controls how long messages should be invisible to other callers when receiving messages from the queue
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MessageInvisibleTime(this Configure config, int value)
        {
            SettingsHolder.SetProperty<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, value);

            return config;
        }

        /// <summary>
        /// Controls how many messages should be read from the queue at once
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure BatchSize(this Configure config, int value)
        {
            SettingsHolder.SetProperty<AzureMessageQueueReceiver>(t => t.BatchSize, value);
        
            return config;
        }

        /// <summary>
        /// Configures a queue per instance
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure QueuePerInstance(this Configure config)
        {
            SettingsHolder.Set("AzureMessageQueueReceiver.QueuePerInstance", true);
            return config;
        }
    }
}