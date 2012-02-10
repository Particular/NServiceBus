namespace NServiceBus.Distributor.Installers
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Utils;

    public class WorkerInputInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        /// <summary>
        /// Install queue for Worker message handler. Will not install if Distributor is not configured to run on this endpoint or if the worker should not run on this endpoint.
        /// </summary>
        /// <param name="identity"></param>
        public void Install(WindowsIdentity identity)
        {
            if ((!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint()) || (!Configure.Instance.WorkerShouldRunOnDistributorEndpoint()))
                return;

            //create the worker queue
            MsmqUtilities.CreateQueueIfNecessary(Address.Local.SubScope("Worker"), identity.Name);
        }
    }
}
