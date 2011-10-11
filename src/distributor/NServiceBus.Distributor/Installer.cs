namespace NServiceBus.Distributor
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode || !Configure.Instance.Configurer.HasComponent<DistributorReadyMessageProcessor>())
                return;

            var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

            MsmqUtilities.CreateQueueIfNecessary(m.ControlQueue, identity.Name);

        }
    }
}
