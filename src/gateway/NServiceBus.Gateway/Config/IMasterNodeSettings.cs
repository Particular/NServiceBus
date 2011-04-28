namespace NServiceBus.Gateway.Config
{
    using NServiceBus.Faults;
    using NServiceBus.Unicast.Queuing;

    public interface IMasterNodeSettings
    {
        IReceiveMessages Receiver { get;}
        int NumberOfWorkerThreads { get;}
        int MaxRetries { get;}
        IManageMessageFailures FailureManager { get;}
        string AddressOfAuditStore { get; }
    }
}