using NServiceBus;

namespace OrderService
{
    public class DisableSattelites : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance
                     .DisableGateway()
                     .DisableNotifications()
                     .DisableSecondLevelRetries()
                     .DisableTimeoutManager();
        }
    }
}