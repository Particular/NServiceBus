namespace NServiceBus.Distributor.Installers
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Utils;

    public class ControlQueueInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;

            //create the control queue
            var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

            MsmqUtilities.CreateQueueIfNecessary(m.ControlQueue, identity.Name);

        }
    }


}