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

            MessageQueue queue = null;
            try
            {
                if (!MsmqUtilities.TryOpenQueue(msmqAddress, out queue) && MsmqUtilities.TryCreateQueue(msmqAddress, identity, settings.UseTransactionalQueues, out queue))
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
            finally
            {
                queue?.Dispose();
            }
        }

        MsmqSettings settings;

        static ILog Logger = LogManager.GetLogger<QueueCreator>();
    }
}