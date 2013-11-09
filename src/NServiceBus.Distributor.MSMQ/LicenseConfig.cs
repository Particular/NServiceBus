namespace NServiceBus.Distributor.MSMQ
{
    using System.Collections.Concurrent;
    using System.Linq;
    using Licensing;
    using Logging;

    /// <summary>
    ///     Limit number of workers in accordance with Licensing policy
    /// </summary>
    internal static class LicenseConfig
    {
        static LicenseConfig()
        {
            allowedWorkerNodes = LicenseManager.CurrentLicense.AllowedNumberOfWorkerNodes;
        }

        internal static bool LimitNumberOfWorkers(Address workerAddress)
        {
            if (WorkersList.Contains(workerAddress))
            {
                return false;
            }

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

        static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseConfig));
        static readonly int allowedWorkerNodes;
        static readonly ConcurrentBag<Address> WorkersList = new ConcurrentBag<Address>();
    }
}