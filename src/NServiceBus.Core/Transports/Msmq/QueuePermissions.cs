namespace NServiceBus.Transports.Msmq
{
    using System.Diagnostics;
    using System.Messaging;
    using System.Security;
    using System.Security.Principal;
    using Logging;

    static class QueuePermissions
    {
        public static void CheckQueue(string address)
        {
            var msmqAddress = MsmqAddress.Parse(address);
            var queuePath = msmqAddress.PathWithoutPrefix;

            Logger.Debug($"Checking if queue exists: {queuePath}.");

            if (msmqAddress.IsRemote)
            {
                Logger.Debug("Queue is on remote machine.");
                Logger.Debug("If this does not succeed (like if the remote machine is disconnected), processing will continue.");
            }

            var path = msmqAddress.PathWithoutPrefix;
            try
            {
                if (MessageQueue.Exists(path))
                {
                    using (var messageQueue = new MessageQueue(path))
                    {
                        Logger.DebugFormat("Verified that the queue: [{0}] existed", queuePath);

                        WarnIfPublicAccess(messageQueue);
                    }
                }
            }
            catch (MessageQueueException)
            {
                // Can happen because of an invalid queue path or trying to access a remote private queue.
                // Either way, this results in a failed attempt, therefore returning false.
                Logger.Warn($"Unable to open the queue at address '{queuePath}'. Make sure the queue exists, and the address is correct. Processing will still continue.");
            }
        }

        static void WarnIfPublicAccess(MessageQueue queue)
        {
            MessageQueueAccessRights? everyoneRights, anonymousRights;

            try
            {
                queue.TryGetPermissions(LocalAnonymousLogonName, out anonymousRights);
                queue.TryGetPermissions(LocalEveryoneGroupName, out everyoneRights);
            }
            catch (SecurityException se)
            {
                Logger.Warn($"Unable to read permissions for queue [{queue.QueueName}]. Make sure you have administrative access on the target machine", se);
                return;
            }

            if (anonymousRights.HasValue || everyoneRights.HasValue)
            {
                var logMessage = $"Queue [{queue.QueueName}] is running with [{LocalEveryoneGroupName}] and/or [{LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by the organization. For more information, consult the documentation.";

                if (Debugger.IsAttached)
                {
                    Logger.Info(logMessage);
                }
                else
                {
                    Logger.Warn(logMessage);
                }
            }
        }

        internal static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        internal static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

        static ILog Logger = LogManager.GetLogger(typeof(QueuePermissions));
    }
}
