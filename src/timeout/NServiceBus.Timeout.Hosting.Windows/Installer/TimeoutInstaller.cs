using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Timeout.Hosting.Windows.Installer
{
    using System.Security.Principal;

    public class TimeoutInstaller : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        /// <summary>
        /// Install Timeout manager queue if TimeoutManager is enabled.
        /// </summary>
        /// <param name="identity"></param>
        public void Create(WindowsIdentity identity)
        {
            if ((ConfigureTimeoutManager.TimeoutManagerAddress != null) && (Configure.Instance.IsTimeoutManagerEnabled()))
                Creator.CreateQueueIfNecessary(ConfigureTimeoutManager.TimeoutManagerAddress, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}