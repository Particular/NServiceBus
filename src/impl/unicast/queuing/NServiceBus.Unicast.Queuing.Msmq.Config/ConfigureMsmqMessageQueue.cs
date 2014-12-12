using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus
{
    using Unicast.Queuing.Msmq.Config.Installers;

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

            if (!Configure.SendOnlyMode)
            {
                config.Configurer.ConfigureComponent<MsmqMessageReceiver>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                    .ConfigureProperty(p => p.ErrorQueue, config.GetConfiguredErrorQueue());
            }

            config.Configurer.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.SingleInstance);

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

            EndpointInputQueueInstaller.Enabled = true;
            MsmqInfrastructureInstaller.Enabled = true;

            return config;
        }


    }
}
