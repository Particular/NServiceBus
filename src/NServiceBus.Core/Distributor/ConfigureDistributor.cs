namespace NServiceBus
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static bool DistributorConfiguredToRunOnThisEndpoint(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static bool WorkerRunsOnThisEndpoint(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        /// <summary>
        /// Configure the distributor to run on this endpoint
        /// </summary>
        /// <param name="withWorker">True if this endpoint should enlist as a worker</param>
        public static Configure RunDistributor(this Configure config, bool withWorker = true)
        {
            throw new Exception("Obsolete");
        }

        /// <summary>
        /// Starting the Distributor without a worker running on its endpoint
        /// </summary>
        public static Configure RunDistributorWithNoWorkerOnItsEndpoint(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        /// <summary>
        /// Enlist Worker with Master node defined in the config.
        /// </summary>
        public static Configure EnlistWithDistributor(this Configure config)
        {
            throw new Exception("Obsolete");
        }
    }
}