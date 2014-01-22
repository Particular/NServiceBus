namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;
    using Config;
    using Logging;
    using Support;

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

            var isRemote = address.Machine.ToLower() != RuntimeEnvironment.MachineName.ToLower();
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
        public static string GetFullPathWithoutPrefix(Address address)
        {
            var queueName = address.Queue;
            var msmqTotalQueueName = address.Machine + MsmqUtilities.PRIVATE + queueName;

            if (msmqTotalQueueName.Length >= 124)
            {
                msmqTotalQueueName = address.Machine + MsmqUtilities.PRIVATE + DeterministicGuidBuilder(queueName).ToString();
            }

            return msmqTotalQueueName;
        }

        private static Guid DeterministicGuidBuilder(string input)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }

}