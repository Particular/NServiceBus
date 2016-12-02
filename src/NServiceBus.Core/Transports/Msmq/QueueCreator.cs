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

            if (msmqAddress.IsRemote)
            {
                Logger.Info($"'{address}' is a remote queue and won't be created");
                return;
            }

            var queuePath = msmqAddress.PathWithoutPrefix;

            if (MessageQueue.Exists(queuePath))
            {
                Logger.Debug($"'{address}' already exists");
                return;
            }

            try
            {
                using (var queue = MessageQueue.Create(queuePath, useTransactionalQueues))
                {
                    try
                    {
                        Logger.Debug("Setting queue permissions.");

                        QueuePermissions.SetPermissionsForQueue(queue, identity);
                    }
                    catch (MessageQueueException ex)
                    {
                        Logger.Error($"Unable to set permissions for queue {queue.QueueName}", ex);
                    }
                }
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
                {
                    //Solve the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
                    return;
                }

                Logger.Error($"Could not create queue {msmqAddress}. Processing will still continue.", ex);
            }
        }

        bool useTransactionalQueues;

        static ILog Logger = LogManager.GetLogger<QueueCreator>();
    }
}