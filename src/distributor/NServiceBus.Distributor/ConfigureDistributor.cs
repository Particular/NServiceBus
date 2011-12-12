namespace NServiceBus
{
    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled(this Configure config)
        {
            return distributorEnabled;
        }


        public static bool DistributorShouldRunOnThisEndpoint(this Configure config)
        {
            return distributorEnabled && config.IsConfiguredAsMasterNode();
        }


        public static Configure UseDistributor(this Configure config)
        {
            distributorEnabled = true;

            return config;
        }

        static bool distributorEnabled;
    }
}