namespace NServiceBus.Gateway.Config
{
    using Faults;
    using Unicast.Queuing;

    public interface IMainEndpointSettings
    {
        IReceiveMessages Receiver { get;}
        int NumberOfWorkerThreads { get;}
        int MaxRetries { get;}
        IManageMessageFailures FailureManager { get;}
        Address AddressOfAuditStore { get; }
    }
}