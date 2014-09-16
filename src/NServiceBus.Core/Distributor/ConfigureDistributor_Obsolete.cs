#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
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

        public static Configure RunDistributor(this Configure config, bool withWorker = true)
        {
            throw new Exception("Obsolete");
        }

        public static Configure RunDistributorWithNoWorkerOnItsEndpoint(this Configure config)
        {
            throw new Exception("Obsolete");
        }

        public static Configure EnlistWithDistributor(this Configure config)
        {
            throw new Exception("Obsolete");
        }
    }
}
