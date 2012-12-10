using NServiceBus;
using NServiceBus.Hosting.Azure;

namespace Worker
{
    public class WorkerRole : RoleEntryPoint
    {
      
    }

    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With(AllAssemblies.Except("NServiceBus.Hosting.Azure.HostProcess.exe"));
        }
    }
}
