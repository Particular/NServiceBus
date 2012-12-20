namespace NServiceBus
{
    using Config;
    using Unicast.Queuing.Installers;
    using Unicast.Queuing.Msmq;

    public static class ConfigureMsmqMessageQueue
    {
        /// <summary>
        /// Indicates that MsmqMessageQueue has been selected.
        /// </summary>
        public static bool Selected { get; set; }

        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure MsmqTransport(this Configure config)
        {
            Selected = true;

            config.Configurer.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MsmqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
            config.Configurer.ConfigureComponent<MsmqQueueCreator>(DependencyLifecycle.SingleInstance);

            var cfg = Configure.GetConfigSection<MsmqMessageQueueConfig>();

            var useJournalQueue = false;
            var useDeadLetterQueue = true;

            if (cfg != null)
            {
                useJournalQueue = cfg.UseJournalQueue;
                useDeadLetterQueue = cfg.UseDeadLetterQueue;
            }

            config.Configurer.ConfigureProperty<MsmqMessageSender>(t => t.UseDeadLetterQueue, useDeadLetterQueue);
            config.Configurer.ConfigureProperty<MsmqMessageSender>(t => t.UseJournalQueue, useJournalQueue);

            EndpointInputQueueCreator.Enabled = true;

            return config;
        }
    }
}