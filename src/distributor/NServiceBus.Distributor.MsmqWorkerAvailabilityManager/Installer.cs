using System.Security.Principal;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
{
    ///<summary>
    /// Creates the queue to store worker availability information.
    ///</summary>
    public class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        /// <summary>
        /// Implementation of INeedToInstallSomething.Install
        /// </summary>
        /// <param name="identity"></param>
        public void Install(WindowsIdentity identity)
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
                return;
            var m = Configure.Instance.Builder.Build<MsmqWorkerAvailabilityManager>();

            MsmqUtilities.CreateQueueIfNecessary(m.StorageQueue, identity.Name);

        }
    }
}
