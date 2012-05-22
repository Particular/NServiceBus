using System;
using NServiceBus.Licensing;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    public static class LicenceConfig
    {
        ///// <summary>
        ///// How many worker (message receiving) threads are allowed by licensing policy
        ///// </summary>
        public const string WorkerThreadsLicenseKey = "WorkerThreads";
        public const string MaXWorkerThreads = "Max";
        public const string SingleWorkerThread = "1";
        public const int MaxNumberOfWorkerThreads = 1024;
        
        internal static int GetAllowedNumberOfThreads(int numberOfWorkerThreadsInConfig)
        {
            if (numberOfWorkerThreadsInConfig < 0)
                return numberOfWorkerThreadsInConfig;

            var license = LicenseManager.CurrentLicense;
            if (license.LicenseType == LicenseType.Basic1)
                return int.Parse(SingleWorkerThread);

       
            if (license.LicenseAttributes == null)
                return numberOfWorkerThreadsInConfig;
            
            if (license.LicenseAttributes.ContainsKey(WorkerThreadsLicenseKey))
            {
                string workerThreadsInLicenseFile = license.LicenseAttributes[WorkerThreadsLicenseKey];

                if (string.IsNullOrWhiteSpace(workerThreadsInLicenseFile))
                    return int.Parse(SingleWorkerThread);

                if (workerThreadsInLicenseFile == MaXWorkerThreads)
                    return Math.Min(MaxNumberOfWorkerThreads, numberOfWorkerThreadsInConfig);

                int workerThreads;
                if(int.TryParse(workerThreadsInLicenseFile, out workerThreads))
                    return Math.Min(workerThreads, numberOfWorkerThreadsInConfig);
                return numberOfWorkerThreadsInConfig;
            }
            return numberOfWorkerThreadsInConfig;
        }

    }
}
