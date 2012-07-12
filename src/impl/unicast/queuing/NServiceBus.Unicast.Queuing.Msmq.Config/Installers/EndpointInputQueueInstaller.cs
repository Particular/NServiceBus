namespace NServiceBus.Unicast.Queuing.Msmq.Config.Installers
{
    using System.Security.Principal;
    using Installation;
    using Utils;
    using NServiceBus.Config;

    public class EndpointInputQueueInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public static bool Enabled { get; set; }

        public void Install(WindowsIdentity identity)
        {
            if (!Enabled)
                return;

            MsmqUtilities.CreateQueueIfNecessary(Address.Local, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();
            if (unicastConfig != null)
                if (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo))
                    MsmqUtilities.CreateQueueIfNecessary(Address.Parse(unicastConfig.ForwardReceivedMessagesTo), identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}
