namespace NServiceBus.Features
{
    using Logging;
    using Persistence.Legacy;
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
            var queueName = context.Settings.GetConfiguredMsmqPersistenceSubscriptionQueue();

            if (string.IsNullOrEmpty(queueName))
            {
                queueName = "NServiceBus.Subscriptions";
                Logger.Warn($"The queue used to store subscriptions has not been configured, so the default '{queueName}' will be used.");
            }

            context.Settings.Get<QueueBindings>().BindSending(queueName);

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