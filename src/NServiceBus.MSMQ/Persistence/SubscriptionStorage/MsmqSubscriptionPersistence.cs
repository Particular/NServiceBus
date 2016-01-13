namespace NServiceBus.Features
{
    using Config;
    using Logging;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides subscription storage using a msmq queue as the backing store.
    /// </summary>
    public class MsmqSubscriptionPersistence:Feature
    {
        internal MsmqSubscriptionPersistence()
        {
        }
        /// <summary>
        /// Invoked if the feature is activated.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected override void Setup(FeatureConfigurationContext context)
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

            context.Container.ConfigureComponent(b =>
            {
                var isTransactional = context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;

                var queue = new MsmqSubscriptionStorageQueue(MsmqAddress.Parse(queueName), isTransactional, false);
                return new MsmqSubscriptionStorage(queue);
            }, DependencyLifecycle.SingleInstance);
        }

        static ILog Logger = LogManager.GetLogger(typeof(MsmqSubscriptionPersistence));
    }
}