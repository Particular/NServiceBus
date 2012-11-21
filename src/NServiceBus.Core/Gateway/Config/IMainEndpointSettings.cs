namespace NServiceBus.Gateway.Config
{
    using Faults;
    using Unicast.Queuing;

    public interface IMainEndpointSettings
    {
        int NumberOfWorkerThreads { get;}
        IManageMessageFailures FailureManager { get;}
        Address AddressOfAuditStore { get; }
    }
}