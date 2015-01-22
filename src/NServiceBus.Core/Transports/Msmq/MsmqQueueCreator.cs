namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using Config;
    using Logging;
    using Support;

    class MsmqQueueCreator : ICreateQueues
    {
        static ILog Logger = LogManager.GetLogger<MsmqQueueCreator>();
        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

        /// <summary>
        /// The current runtime settings
        /// </summary>
        public MsmqSettings Settings { get; set; }

        ///<summary>
        /// Utility method for creating a queue if it does not exist.
        ///</summary>
        ///<param name="address">Queue path to create</param>
        ///<param name="account">The account to be given permissions to the queue</param>
        public void CreateQueueIfNecessary(Address address, string account)
        {
            if (address == null)
            {
                return;
            }

            var queuePath = GetFullPathWithoutPrefix(address);
            var isRemote = address.Machine.ToLower() != RuntimeEnvironment.MachineName.ToLower();
            
            if (isRemote)
            {
                Logger.Debug("Queue is on remote machine.");
                Logger.Debug("If this does not succeed (like if the remote machine is disconnected), processing will continue.");
            }

            Logger.Debug(String.Format("Checking if queue exists: {0}.", address));

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

                CreateQueue(queuePath, account, Settings.UseTransactionalQueues);
            }
            catch (MessageQueueException ex)
            {
                if (isRemote && (ex.MessageQueueErrorCode == MessageQueueErrorCode.IllegalQueuePathName))
                {
                    return;
                }
                Logger.Error(String.Format("Could not create queue {0} or check its existence. Processing will still continue.", address), ex);
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Could not create queue {0} or check its existence. Processing will still continue.", address), ex);
            }
        }

        /// <summary>
        /// Returns the full path without Format or direct os
        /// from an address.
        /// </summary>
        public static string GetFullPathWithoutPrefix(Address address)
        {
            return GetFullPathWithoutPrefix(address.Queue, address.Machine);
        }

        public static string GetFullPathWithoutPrefix(string queue, string machine)
        {
            return machine + NServiceBus.MsmqUtilities.PRIVATE + queue;
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
    }
}
