using System.Security.Principal;
using NServiceBus.Installation;
using NServiceBus.Utils;
using NServiceBus;

namespace NServiceBus.Unicast.Subscriptions.Msmq.Config
{
    /// <summary>
    /// Class responsible for installing the MSMQ subscription storage.
    /// </summary>
    public class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        /// <summary>
        /// Installs the queue.
        /// </summary>
        /// <param name="identity"></param>
        public void Install(WindowsIdentity identity)
        {
            MsmqUtilities.CreateQueueIfNecessary(ConfigureMsmqSubscriptionStorage.Queue, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}
