namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Messaging;
    using System.Security;
    using Logging;

    class QueuePermissionChecker
    {
        public void CheckQueuePermissions(IReadOnlyCollection<string> queues)
        {
            foreach (var address in queues)
            {
                CheckQueue(address);
            }
        }

        static void CheckQueue(string address)
        {
            MessageQueue messageQueue;
            if (!MsmqUtilities.TryOpenQueue(address, out messageQueue))
            {
                Logger.Warn($"Unable to open the queue at address '{address}'. Make sure the queue exists locally, and the address is correct.");
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
                queue.TryGetPermissions(QueueCreator.LocalAnonymousLogonName, out anonymousRights);
                queue.TryGetPermissions(QueueCreator.LocalEveryoneGroupName, out everyoneRights);
            }
            catch (SecurityException se)
            {
                Logger.Warn($"Unable to read permissions for queue [{queue.QueueName}]. Make sure you have administrative access on the target machine", se);
                return;
            }

            if (anonymousRights.HasValue && everyoneRights.HasValue)
            {
                var logMessage = $"Queue [{queue.QueueName}] is running with [{QueueCreator.LocalEveryoneGroupName}] and [{QueueCreator.LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by the organization. For more information, consult the documentation.";

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

        static ILog Logger = LogManager.GetLogger<QueuePermissionChecker>();
    }
}