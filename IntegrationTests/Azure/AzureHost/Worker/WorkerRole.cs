using NServiceBus;
using NServiceBus.Hosting.Azure;

namespace Worker
{
    public class WorkerRole : RoleEntryPoint
    {
      
    }

    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureStorageQueue>
    {
        
    }
}
