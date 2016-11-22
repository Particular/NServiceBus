namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Transport;
    using Transports.Msmq;

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
                    Logger.DebugFormat($"Created queue, path: [{queuePath}], identity: [{identity}], transactional: [{useTransactionalQueues}]");

                    SetPermissions(queue, LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl);

                    SetPermissions(queue, identity, MessageQueueAccessRights.WriteMessage);
                    SetPermissions(queue, identity, MessageQueueAccessRights.ReceiveMessage);
                    SetPermissions(queue, identity, MessageQueueAccessRights.PeekMessage);

                    // Everyone & Anonymous is granted write message permissions on all Windows / Windows Server versions after 2003.
                    // But we keep this for now for backwards compatibility
                    SetPermissions(queue, QueuePermissions.LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage);
                    SetPermissions(queue, QueuePermissions.LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage);
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

        static void SetPermissions(MessageQueue queue, string identity, MessageQueueAccessRights accessRights)
        {
            try
            {
                queue.SetPermissions(identity, accessRights, AccessControlEntryType.Allow);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to give '{identity}' {accessRights} access to queue '{queue.FormatName}'", ex);
            }
        }

        bool useTransactionalQueues;

        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        static ILog Logger = LogManager.GetLogger<QueueCreator>();
    }
}