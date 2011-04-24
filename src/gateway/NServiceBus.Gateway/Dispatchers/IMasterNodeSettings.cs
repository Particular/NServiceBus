namespace NServiceBus.Gateway.Dispatchers
{
    using Faults;
    using Unicast.Queuing;

    public interface IMasterNodeSettings
    {
        IReceiveMessages Receiver { get;}
        int NumberOfWorkerThreads { get;}
        int MaxRetries { get;}
        IManageMessageFailures FailureManager { get;}
        string AddressOfAuditStore { get; }
    }
}