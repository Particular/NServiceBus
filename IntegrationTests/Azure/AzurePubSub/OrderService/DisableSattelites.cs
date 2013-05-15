using NServiceBus;
using NServiceBus.Features;

namespace OrderService
{
    public class DisableSattelites : IWantCustomInitialization
    {
        public void Init()
        {
            Feature.Disable<Gateway>();
            Feature.Disable<SecondLevelRetries>();
            Feature.Disable<TimeoutManager>();
        }
    }
}