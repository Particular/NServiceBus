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
            if (DistributorActivated)
            {
                var m = Configure.Instance.Builder.Build<MsmqWorkerAvailabilityManager>();

                MsmqUtilities.CreateQueueIfNecessary(m.StorageQueue, identity.Name);
            }
        }

        /// <summary>
        /// Indication that the distributor has been activated for this endpoint.
        /// </summary>
        public static bool DistributorActivated { get; set; }
    }
}
