using System;
using System.Collections.Generic;
using NServiceBus.Licensing;
using log4net;

namespace NServiceBus.Distributor
{
    /// <summary>
    /// Limit number of workers in accordance with Licensing policy
    /// </summary>
    public static class LicenseDistributor
    {
        public const string AllowedNumberOfWorkerNodesLicenseKey = "AllowedNumberOfWorkerNodes";
        public const int UnlimitedNumberOfWorkerNodes = 0;
        public const int MinNumberOfWorkerNodes = 2;

        private static readonly ILog Logger = LogManager.GetLogger("Distributor." + Configure.EndpointName);
        private static bool? licenseLimitationOnNumberOfWorkers;
        private static readonly List<Address> WorkersList = new List<Address>();
        private static int allowedWorkerNodes = MinNumberOfWorkerNodes;

        internal static bool LimitNumberOfWorkers(Address workerAddress)
        {
            lock (WorkersList)
            {
                if (licenseLimitationOnNumberOfWorkers == false)
                    return false;

                if (licenseLimitationOnNumberOfWorkers == true)
                    return LimitAddingAWorker(workerAddress);

                var currentLicense = LicenseManager.CurrentLicense;

                if (currentLicense.LicenseAttributes.ContainsKey(AllowedNumberOfWorkerNodesLicenseKey))
                {
                    allowedWorkerNodes =
                        int.Parse(currentLicense.LicenseAttributes[AllowedNumberOfWorkerNodesLicenseKey]);
                    if (allowedWorkerNodes == UnlimitedNumberOfWorkerNodes)
                        licenseLimitationOnNumberOfWorkers = false;
                }
                if ((currentLicense.LicenseType == LicenseType.Standard) ||
                    (currentLicense.LicenseType == LicenseType.Trial))
                {
                    licenseLimitationOnNumberOfWorkers = false;
                    return false;
                }

                licenseLimitationOnNumberOfWorkers = true;
                allowedWorkerNodes = MinNumberOfWorkerNodes;
                return LimitAddingAWorker(workerAddress);
            }
        }
        /// <summary>
        /// Check if allowed to register a Worker.
        /// </summary>
        /// <param name="workerAddress"></param>
        /// <returns>True to limit adding a Worker</returns>
        private static bool LimitAddingAWorker(Address workerAddress)
        {
            if (WorkersList.Contains(workerAddress))
                return false;
            
            lock (WorkersList)
            {
                if (WorkersList.Count < allowedWorkerNodes)
                {
                    WorkersList.Add(workerAddress);
                    return false;
                }
                Logger.WarnFormat(
                    "License limitation for [{0}] workers per distributor reached. To obtain a license that allows to add more workers, please visit http://nservicebus.com/License.aspx.",
                    allowedWorkerNodes);
                return true;
            }
        }
    }
}
