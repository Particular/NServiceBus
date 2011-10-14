namespace NServiceBus.Distributor
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
                return;

            //create the main input queue
             var mainInputAddress = Address.Parse(Address.Local.ToString().Replace(".worker", ""));

             MsmqUtilities.CreateQueueIfNecessary(mainInputAddress, identity.Name);


            //create the control queue
            var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

            MsmqUtilities.CreateQueueIfNecessary(m.ControlQueue, identity.Name);

        }
    }
}
