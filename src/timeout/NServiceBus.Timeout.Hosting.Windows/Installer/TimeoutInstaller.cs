namespace NServiceBus.Timeout.Hosting.Windows.Installer
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    public class TimeoutInstaller : INeedToInstallSomething<Installation.Environments.Windows>
    {
        /// <summary>
        /// Install Timeout manager queue if TimeoutManager is enabled.
        /// </summary>
        /// <param name="identity"></param>
        public void Install(WindowsIdentity identity)
        {
            if ((ConfigureTimeoutManager.TimeoutManagerAddress != null) && (Configure.Instance.IsTimeoutManagerEnabled()))
                MsmqUtilities.CreateQueueIfNecessary(ConfigureTimeoutManager.TimeoutManagerAddress, identity.Name);
        }
    }
}