using NServiceBus;
using NServiceBus.Hosting.Azure;

namespace Worker
{
    public class WorkerRole : RoleEntryPoint, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.UseInMemoryTimeoutPersister();
        }
    }
}
