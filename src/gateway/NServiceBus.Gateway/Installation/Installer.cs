namespace NServiceBus.Gateway.Installation
{
    using System.Security.Principal;
    using Unicast.Queuing;

    public class Installer : IWantQueuesCreated<NServiceBus.Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        public void Create(WindowsIdentity identity)
        {
            if (ConfigureGateway.GatewayInputAddress != null)
                Creator.CreateQueueIfNecessary(ConfigureGateway.GatewayInputAddress, identity.Name);
        }
    }
}