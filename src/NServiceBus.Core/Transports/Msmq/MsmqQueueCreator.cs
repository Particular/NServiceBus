namespace NServiceBus
{
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class MsmqQueueCreator : ICreateQueues
    {
        public MsmqQueueCreator(bool useTransactionalQueues)
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
                    Logger.Debug($"Created queue, path: [{queuePath}], identity: [{identity}], transactional: [{useTransactionalQueues}]");

                    try
                    {
                        queue.SetPermissions(identity, MessageQueueAccessRights.WriteMessage);
                        queue.SetPermissions(identity, MessageQueueAccessRights.ReceiveMessage);
                        queue.SetPermissions(identity, MessageQueueAccessRights.PeekMessage);

                        queue.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl);
                    }
                    catch (MessageQueueException permissionException) when (permissionException.MessageQueueErrorCode == MessageQueueErrorCode.FormatNameBufferTooSmall)
                    {
                        Logger.Warn($"Queue '{queue.FormatName}' has a to long name for permissions to be applied. Please consider a shorter endpoint name.", permissionException);
                    }

                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
            {
                //Solves the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
            }
        }

        bool useTransactionalQueues;

        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        static ILog Logger = LogManager.GetLogger<MsmqQueueCreator>();
    }
}