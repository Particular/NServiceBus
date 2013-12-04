namespace NServiceBus.Distributor
{
    using System.Collections.Concurrent;
    using System.Linq;
    using Licensing;
    using Logging;

    /// <summary>
    /// Limit number of workers in accordance with Licensing policy
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public static class LicenseConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Distributor." + Configure.EndpointName);
        private static readonly int allowedWorkerNodes;
        private static readonly ConcurrentBag<Address> WorkersList = new ConcurrentBag<Address>();

        static LicenseConfig()
        {
            allowedWorkerNodes = LicenseManager.License.AllowedNumberOfWorkerNodes;
        }

        internal static bool LimitNumberOfWorkers(Address workerAddress)
        {
            if (WorkersList.Contains(workerAddress))
                return false;

            if (WorkersList.Count < allowedWorkerNodes)
            {
                WorkersList.Add(workerAddress);
                return false;
            }
            Logger.WarnFormat(
                "License limitation for [{0}] workers per distributor reached. To obtain a license that allows to add more workers, please visit http://particular.net/licensing",
                allowedWorkerNodes);
            return true;
        }
    }
}
