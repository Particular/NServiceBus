namespace NServiceBus
{
    using System;

    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled(this Configure config)
        {
            return distributorEnabled;
        }


        public static bool DistributorConfiguredToRunOnThisEndpoint(this Configure config)
        {
            return distributorEnabled && distributorShouldRunOnThisEndpoint;
        }

        public static Configure RunDistributor(this Configure config)
        {
            if (!config.IsConfiguredAsMasterNode())
                throw new InvalidOperationException("This endpoint needs to be configured as a master node in order to run the distributor");

            distributorEnabled = true;
            distributorShouldRunOnThisEndpoint = true;

            return config;
        }


        public static Configure EnlistWithDistributor(this Configure config)
        {
            distributorEnabled = true;

            return config;
        }

        static bool distributorEnabled;
        static bool distributorShouldRunOnThisEndpoint;
    }
}