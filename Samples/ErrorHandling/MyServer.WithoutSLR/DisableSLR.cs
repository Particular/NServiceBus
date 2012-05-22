using NServiceBus;
using NServiceBus.Config;

namespace MyServerNoSLR
{
    public class DisableSLR : INeedInitialization
    {
        public void Init()
        {
            // Using code we disable the second level retries.            
            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}