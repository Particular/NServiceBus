using NServiceBus;
using NServiceBus.Hosting.Azure;

namespace OrderService
{
    public class Host : RoleEntryPoint, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.UseInMemoryTimeoutPersister();
        }
    }
}