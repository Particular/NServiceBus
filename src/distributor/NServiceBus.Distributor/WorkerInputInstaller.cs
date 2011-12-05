namespace NServiceBus.Distributor
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class WorkerInputInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!ConfigureDistributor.DistributorShouldRunOnThisEndpoint())
                return;

            //create the worker queue
            MsmqUtilities.CreateQueueIfNecessary(Address.Local.SubScope("Worker"), identity.Name);
        }
    }
}
