using NServiceBus;

namespace MyServerNoSLR
{
    public class BeforeConfigurationIsFinalized : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            // Here, using code, we disable the second level retries.            
            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}