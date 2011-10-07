namespace NServiceBus.Distributor
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (DistributorActivated)
            {
                var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

                MsmqUtilities.CreateQueueIfNecessary(m.ControlQueue, identity.Name);
            }
        }

        public static bool DistributorActivated { get; set; }
    }
}
