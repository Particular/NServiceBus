using NServiceBus;

namespace MyServerNoSLR
{
    using NServiceBus.Features;

    public class DisableSLR : INeedInitialization
    {
        public void Init()
        {
            // Using code we disable the second level retries.            
            Configure.Features.Disable<SecondLevelRetries>();
        }
    }
}