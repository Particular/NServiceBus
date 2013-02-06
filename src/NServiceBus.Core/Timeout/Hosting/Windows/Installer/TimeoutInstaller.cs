namespace NServiceBus.Timeout.Hosting.Windows.Installer
{
    using Unicast.Queuing;

    public class TimeoutInstaller : IWantQueueCreated
    {
        public Address Address { get { return ConfigureTimeoutManager.TimeoutManagerAddress; } }
        public bool IsDisabled { get { return !TimeoutManager.Enabled; } }
    }

    public class TimeoutDispatcherInstaller : IWantQueueCreated
    {
        public Address Address { get { return TimeoutDispatcherProcessor.TimeoutDispatcherAddress; } }
        public bool IsDisabled { get { return !TimeoutManager.Enabled; } }
    }
}