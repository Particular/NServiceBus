using System.Security.Principal;
using NServiceBus.Config;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq.Config
{
    class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!ConfigureMsmqMessageQueue.Selected)
                return;

            MsmqUtilities.CreateQueueIfNecessary(Address.Local, identity.Name);

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();
            if (unicastConfig != null)
                if (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo))
                    MsmqUtilities.CreateQueueIfNecessary(Address.Parse(unicastConfig.ForwardReceivedMessagesTo), identity.Name);
        }
    }
}
