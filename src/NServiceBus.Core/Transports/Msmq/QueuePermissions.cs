﻿namespace NServiceBus
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
                Logger.Info($"Since {address} is remote, the queue could not be verified. Make sure the queue exists and that the address and permissions are correct. Messages could end up in the dead letter queue if configured incorrectly.");
                return;
            }

            var path = msmqAddress.PathWithoutPrefix;

            try
            {
                if (MessageQueue.Exists(path))
                {
                    using (var messageQueue = new MessageQueue(path))
                    {
                        Logger.DebugFormat("Verified that the queue: [{0}] exists", queuePath);

                        WarnIfPublicAccess(messageQueue);
                    }
                }
                else
                {
                    Logger.WarnFormat("Queue [{0}] does not exist", queuePath); 
                }
            }
            catch (MessageQueueException ex)
            {
                Logger.Warn($"Unable to verify queue at address '{queuePath}'. Make sure the queue exists, and that the address is correct. Processing will still continue.", ex);
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
