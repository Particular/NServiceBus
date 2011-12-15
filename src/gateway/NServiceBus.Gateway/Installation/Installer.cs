namespace NServiceBus.Gateway.Installation
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Utils;

    public class Installer : INeedToInstallSomething<NServiceBus.Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (ConfigureGateway.GatewayInputAddress != null)
                MsmqUtilities.CreateQueueIfNecessary(ConfigureGateway.GatewayInputAddress, identity.Name);
        }
    }
}