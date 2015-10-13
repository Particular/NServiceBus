namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using Config;
    using Logging;

    class QueueCreator : ICreateQueues
    {
        static ILog Logger = LogManager.GetLogger<QueueCreator>();

        /// <summary>
        /// The current runtime settings.
        /// </summary>
        MsmqSettings settings;

        public QueueCreator(MsmqSettings settings)
        {
            this.settings = settings;
        }

        ///<summary>
        /// Utility method for creating a queue if it does not exist.
        ///</summary>
        ///<param name="address">Queue path to create</param>
        ///<param name="account">The account to be given permissions to the queue</param>
        public void CreateQueueIfNecessary(string address, string account)
        {
            if (address == null)
            {
                return;
            }
            var msmqAddress = MsmqAddress.Parse(address);
            var queuePath = msmqAddress.PathWithoutPrefix;
            
            if (msmqAddress.IsRemote)
            {
                Logger.Debug("Queue is on remote machine.");
                Logger.Debug("If this does not succeed (like if the remote machine is disconnected), processing will continue.");
            }

            Logger.Debug($"Checking if queue exists: {address}.");

            try
            {
                if (MessageQueue.Exists(queuePath))
                {
                    Logger.Debug("Queue exists, going to set permissions.");
                    SetPermissionsForQueue(queuePath, account);
                    return;
                }

                Logger.Warn("Queue " + queuePath + " does not exist.");
                Logger.Debug("Going to create queue: " + queuePath);

                CreateQueue(queuePath, account, settings.UseTransactionalQueues);
            }
            catch (MessageQueueException ex)
            {
                if (msmqAddress.IsRemote && (ex.MessageQueueErrorCode == MessageQueueErrorCode.IllegalQueuePathName))
                {
                    return;
                }
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueExists)
                {
                    //Solve the race condition problem when multiple endpoints try to create same queue (e.g. error queue).
                    return;
                }
                Logger.Error($"Could not create queue {address} or check its existence. Processing will still continue.", ex);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not create queue {address} or check its existence. Processing will still continue.", ex);
            }
        }
        
        static void CreateQueue(string queuePath, string account, bool transactional)
        {
            using (var queue = MessageQueue.Create(queuePath, transactional))
            {
                SetPermissionsForQueue(queue, account);
            }

            Logger.DebugFormat("Created queue, path: [{0}], account: [{1}], transactional: [{2}]", queuePath, account, transactional);
        }

        static void SetPermissionsForQueue(string queuePath, string account)
        {
            using (var messageQueue = new MessageQueue(queuePath))
            {
                SetPermissionsForQueue(messageQueue, account);
            }
        }

        /// <summary>
        /// Sets default permissions for queue.
        /// </summary>
        static void SetPermissionsForQueue(MessageQueue queue, string account)
        {
            queue.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            queue.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            queue.SetPermissions(LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);

            queue.SetPermissions(account, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            queue.SetPermissions(account, MessageQueueAccessRights.ReceiveMessage, AccessControlEntryType.Allow);
            queue.SetPermissions(account, MessageQueueAccessRights.PeekMessage, AccessControlEntryType.Allow);
        }

        internal static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        internal static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        internal static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

    }
}
