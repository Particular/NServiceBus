namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using Transport;

    /// <summary>
    /// Provides subscription storage using a msmq queue as the backing store.
    /// </summary>
    public class MsmqSubscriptionPersistence : Feature
    {
        internal MsmqSubscriptionPersistence()
        {
        }

        /// <summary>
        /// Invoked if the feature is activated.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var queueName = context.Settings.GetOrDefault<string>("MsmqSubscriptionPersistence.QueueName");

            var cfg = context.Settings.GetConfigSection<MsmqSubscriptionStorageConfig>();

            if (string.IsNullOrEmpty(queueName))
            {
                if (cfg == null)
                {
                    Logger.Warn("Could not find configuration section for Msmq Subscription Storage and no name was specified for this endpoint. Going to default the subscription queue");
                    queueName = "NServiceBus.Subscriptions";
                }
                else
                {
                    queueName = cfg.Queue;
                }
            }

            if (queueName != null)
            {
                context.Settings.Get<QueueBindings>().BindSending(queueName);
            }

            var useTransactionalStorageQueue = true;
            MsmqSettings msmqSettings;

            if (context.Settings.TryGet(out msmqSettings))
            {
                useTransactionalStorageQueue = msmqSettings.UseTransactionalQueues;
            }

            context.Container.ConfigureComponent(b =>
            {
                var queue = new MsmqSubscriptionStorageQueue(MsmqAddress.Parse(queueName), useTransactionalStorageQueue);
                return new MsmqSubscriptionStorage(queue);
            }, DependencyLifecycle.SingleInstance);
        }

        static ILog Logger = LogManager.GetLogger(typeof(MsmqSubscriptionPersistence));
    }
}