namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;

    public interface IActiveMqSchedulerManagementCommands
    {
        void Start();
        void Stop();
        void RequestDeferredMessages(IDestination browseDestination);
        ActiveMqSchedulerManagementJob CreateActiveMqSchedulerManagementJob(string selector);
        void DisposeJob(ActiveMqSchedulerManagementJob job);
        void ProcessJob(ActiveMqSchedulerManagementJob job);
    }
}