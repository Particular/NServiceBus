using NServiceBus.Distributor;

namespace NServiceBus
{
    public static class ConfigureDistributor
    {
        public static Configure DoNotUseDistributors(this Configure config)
        {
            ReadyMessageManager.DoNotUseDistributors = true;

            return config;
        }
    }
}