namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Principal;
    using Config;
    using Logging;

    public class MsmqQueueCreator : ICreateQueues
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqQueueCreator));
        private static readonly string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        private static readonly string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        private static readonly string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

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
                return;

            var q = GetFullPathWithoutPrefix(address);

            var isRemote = address.Machine != Environment.MachineName.ToLower();
            if (isRemote)
            {
                Logger.Debug("Queue is on remote machine.");
                Logger.Debug("If this does not succeed (like if the remote machine is disconnected), processing will continue.");
            }

            Logger.Debug(String.Format("Checking if queue exists: {0}.", address));

            try
            {
                if (MessageQueue.Exists(q))
                {
                    Logger.Debug("Queue exists, going to set permissions.");
                    SetPermissionsForQueue(q, account);
                    return;
                }

                Logger.Warn("Queue " + q + " does not exist.");
                Logger.Debug("Going to create queue: " + q);

                CreateQueue(q, account, Settings.UseTransactionalQueues);
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
        ///<summary>
        /// Create named message queue
        ///</summary>
        ///<param name="queueName">Queue path</param>
        ///<param name="account">The account to be given permissions to the queue</param>
        /// <param name="transactional">If volatileQueues is true then create a non-transactional message queue</param>
        private static void CreateQueue(string queueName, string account, bool transactional)
        {
            MessageQueue.Create(queueName, transactional);

            SetPermissionsForQueue(queueName, account);

            Logger.DebugFormat("Created queue, path: [{0}], account: [{1}], transactional: [{2}]", queueName, account, transactional);
        }
        /// <summary>
        /// Sets default permissions for queue.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="account"></param>
        private static void SetPermissionsForQueue(string queue, string account)
        {
            var q = new MessageQueue(queue);

            q.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            q.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            q.SetPermissions(LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);

            q.SetPermissions(account, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            q.SetPermissions(account, MessageQueueAccessRights.ReceiveMessage, AccessControlEntryType.Allow);
            q.SetPermissions(account, MessageQueueAccessRights.PeekMessage, AccessControlEntryType.Allow);
        }

        /// <summary>
        /// Returns the full path without Format or direct os
        /// from an address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetFullPathWithoutPrefix(Address address)
        {
            return address.Machine + MsmqUtilities.PRIVATE + address.Queue;
        }
        
    }

}