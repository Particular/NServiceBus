namespace NServiceBus.Distributor.Installers
{
    using System.Security.Principal;
    using Unicast.Queuing;

    public class ControlQueueInstaller : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        public void Create(WindowsIdentity identity)
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;

            //create the control queue
            var m = Configure.Instance.Builder.Build<DistributorReadyMessageProcessor>();

            Creator.CreateQueueIfNecessary(m.ControlQueue, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}