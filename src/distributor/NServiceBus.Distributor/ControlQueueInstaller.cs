namespace NServiceBus.Distributor
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class ControlQueueInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!ConfigureDistributor.DistributorShouldRunOnThisEndpoint())
                return;

            //create the control queue
            var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

            MsmqUtilities.CreateQueueIfNecessary(m.ControlQueue, identity.Name);

        }
    }
}