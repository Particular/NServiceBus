namespace NServiceBus.Timeout.Hosting.Windows.Installer
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class TimeoutInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (ConfigureTimeoutManager.TimeoutManagerAddress != null)
                MsmqUtilities.CreateQueueIfNecessary(ConfigureTimeoutManager.TimeoutManagerAddress, identity.Name);
        }
    }
}