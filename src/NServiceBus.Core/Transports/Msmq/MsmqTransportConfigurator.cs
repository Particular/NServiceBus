namespace NServiceBus.Features
{
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Security;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// Used to configure the MSMQ transport.
    /// </summary>
    public class MsmqTransportConfigurator : Feature
    {
        internal MsmqTransportConfigurator()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
            DependsOn<Receiving>();
            RegisterStartupTask<CheckQueuePermissions>();
        }

        class CheckQueuePermissions : FeatureStartupTask
        {
            ReadOnlySettings settings;

            public CheckQueuePermissions(ReadOnlySettings settings)
            {
                this.settings = settings;
            }

            protected override Task OnStart(IBusContext context)
            {
                var queueBindings = settings.Get<QueueBindings>();
                var boundQueueAddresses = queueBindings.ReceivingAddresses.Concat(queueBindings.SendingAddresses);

                foreach (var address in boundQueueAddresses)
                {
                    CheckQueue(address);
                }

                return TaskEx.Completed;
            }

            static void CheckQueue(string address)
            {
                var msmqAddress = MsmqAddress.Parse(address);
                var queuePath = msmqAddress.PathWithoutPrefix;

                if (MessageQueue.Exists(queuePath))
                {
                    using (var messageQueue = new MessageQueue(queuePath))
                    {
                        WarnIfPublicAccess(messageQueue);
                    }
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
                    var logMessage = $"Queue [{queue.QueueName}] is running with [{QueueCreator.LocalEveryoneGroupName}] and [{QueueCreator.LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by your organization. For more information, please consult the documentation.";

                    if (Debugger.IsAttached)
                        Logger.Info(logMessage);
                    else
                        Logger.Warn(logMessage);
                }
            }

            static ILog Logger = LogManager.GetLogger<CheckQueuePermissions>();
        }
        
        /// <summary>
        ///     Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}