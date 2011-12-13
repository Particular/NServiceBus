namespace NServiceBus.Distributor.Installers
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Utils;

    public class WorkerInputInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;

            //create the worker queue
            MsmqUtilities.CreateQueueIfNecessary(Address.Local.SubScope("Worker"), identity.Name);
        }
    }
}
