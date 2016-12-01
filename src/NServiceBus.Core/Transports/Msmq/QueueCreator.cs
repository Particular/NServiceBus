namespace NServiceBus
{
    using System.Messaging;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Transport;

    class QueueCreator : ICreateQueues
    {
        public QueueCreator(bool useTransactionalQueues)
        {
            this.useTransactionalQueues = useTransactionalQueues;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            foreach (var receivingAddress in queueBindings.ReceivingAddresses)
            {
                CreateQueueIfNecessary(receivingAddress, identity);
            }

            foreach (var sendingAddress in queueBindings.SendingAddresses)
            {
                CreateQueueIfNecessary(sendingAddress, identity);
            }

            return TaskEx.CompletedTask;
        }

        void CreateQueueIfNecessary(string address, string identity)
        {
            var msmqAddress = MsmqAddress.Parse(address);

            Logger.Debug($"Creating '{address}' if needed.");

            MessageQueue queue;
            if (MsmqUtilities.TryOpenQueue(msmqAddress, out queue) || MsmqUtilities.TryCreateQueue(msmqAddress, identity, useTransactionalQueues, out queue))
            {
                using (queue)
                {
                    Logger.Debug("Setting queue permissions.");

                    try
                    {
                        QueuePermissions.SetPermissionsForQueue(queue, identity);
                    }
                    catch (MessageQueueException ex)
                    {
                        Logger.Error($"Unable to set permissions for queue {queue.QueueName}", ex);
                    }
                }
            }
        }

        bool useTransactionalQueues;

        static ILog Logger = LogManager.GetLogger<QueueCreator>();
    }
}