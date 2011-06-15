namespace NServiceBus.Gateway.Installation
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Utils;

    public class Installer : INeedToInstallSomething<NServiceBus.Installation.Environments.Windows>
    {
        public Address GatewayInputQueue { get; set; }

        public void Install(WindowsIdentity identity)
        {
            if (!string.IsNullOrEmpty(GatewayInputQueue))
                MsmqUtilities.CreateQueueIfNecessary(GatewayInputQueue, identity.Name);
        }
    }
}