namespace NServiceBus.Features
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

            MessageQueue messageQueue;
            if (!MsmqUtilities.TryOpenQueue(msmqAddress, out messageQueue))
            {
                Logger.Warn($"Unable to open the queue at address '{address}'. Make sure the queue exists, and the address is correct. Processing will still continue.");
                return;
            }

            using (messageQueue)
            {
                WarnIfPublicAccess(messageQueue);
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

            if (anonymousRights.HasValue && everyoneRights.HasValue)
            {
                var logMessage = $"Queue [{queue.QueueName}] is running with [{LocalEveryoneGroupName}] and [{LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by the organization. For more information, consult the documentation.";

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

        public static void SetPermissionsForQueue(MessageQueue queue, string account)
        {

            queue.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);

            queue.SetPermissions(account, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            queue.SetPermissions(account, MessageQueueAccessRights.ReceiveMessage, AccessControlEntryType.Allow);
            queue.SetPermissions(account, MessageQueueAccessRights.PeekMessage, AccessControlEntryType.Allow);
        }

        static string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        static string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

        static ILog Logger = LogManager.GetLogger(typeof(QueuePermissions));
    }
}