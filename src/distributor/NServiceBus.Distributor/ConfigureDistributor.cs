using NServiceBus.Distributor;

namespace NServiceBus
{
    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled { get; private set; }

        public static Configure UseDistributor(this Configure config)
        {
            DistributorEnabled = true;

            return config;
        }
    }
}