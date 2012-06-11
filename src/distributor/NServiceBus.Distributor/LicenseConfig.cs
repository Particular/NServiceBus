using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Licensing;
using NServiceBus.Logging;

namespace NServiceBus.Distributor
{
    /// <summary>
    /// Limit number of workers in accordance with Licensing policy
    /// </summary>
    public static class LicenseConfig
    {
        public const string AllowedNumberOfWorkerNodesLicenseKey = "AllowedNumberOfWorkerNodes";
        public const string UnlimitedNumberOfWorkerNodes = "Max";
        public const int MinNumberOfWorkerNodes = 2;

        private static readonly ILog Logger = LogManager.GetLogger("Distributor." + Configure.EndpointName);
        private static bool licenseLimitationOnNumberOfWorkers;
        private static int allowedWorkerNodes = MinNumberOfWorkerNodes;
        private static readonly ConcurrentBag<Address> WorkersList = new ConcurrentBag<Address>();

        internal static void CheckForLicenseLimitationOnNumberOfWorkerNodes()
        {
            var currentLicense = LicenseManager.CurrentLicense;
            if (currentLicense.LicenseType == LicenseType.Basic1)
            {
                licenseLimitationOnNumberOfWorkers = true;
                return;
            }

            if (currentLicense.LicenseAttributes.ContainsKey(AllowedNumberOfWorkerNodesLicenseKey))
            {
                string allowedNumberOfWorkerNodes =
                    currentLicense.LicenseAttributes[AllowedNumberOfWorkerNodesLicenseKey];
                if (allowedNumberOfWorkerNodes == UnlimitedNumberOfWorkerNodes)
                {
                    licenseLimitationOnNumberOfWorkers = false;
                    return;
                }
                
                if(int.TryParse(allowedNumberOfWorkerNodes, out allowedWorkerNodes))
                    licenseLimitationOnNumberOfWorkers = true;
            }
        }

        internal static bool LimitNumberOfWorkers(Address workerAddress)
        {
            if (!licenseLimitationOnNumberOfWorkers)
                return false;
            
            return LimitAddingAWorker(workerAddress);
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
