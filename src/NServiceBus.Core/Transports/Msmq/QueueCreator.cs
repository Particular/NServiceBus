namespace NServiceBus
{
    using System.Messaging;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Transport;

    class QueueCreator : ICreateQueues
    {
        public QueueCreator(MsmqSettings settings)
        {
            this.settings = settings;
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
            if (address == null)
            {
                return;
            }

            var msmqAddress = MsmqAddress.Parse(address);

            Logger.Debug($"Creating '{address}' if needed.");

            if (msmqAddress.IsRemote)
            {
                Logger.Info($"'{address}' is a remote queue and won't be created");
                return;
            }


            if (MessageQueue.Exists(msmqAddress.PathWithoutPrefix))
            {
                Logger.Debug($"'{address}' already exists");
                return;
            }

            var queuePath = msmqAddress.PathWithoutPrefix;
            try
            {
                using (var messageQueue = MessageQueue.Create(queuePath, settings.UseTransactionalQueues))
                {
                    Logger.DebugFormat($"Created queue, path: [{queuePath}], identity: [{identity}], transactional: [{settings.UseTransactionalQueues}]");

                    QueuePermissions.SetPermissionsForQueue(messageQueue, identity);
                }
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
                {
                    //Solve the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
                    return;
                }

                throw;
            }
        }

        MsmqSettings settings;

        static ILog Logger = LogManager.GetLogger<QueueCreator>();
    }
}