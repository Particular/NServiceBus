namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using Licensing;
    using Configure = NServiceBus.Configure;

    public static class LicenceConfig
    {
        internal static int GetAllowedNumberOfThreads(int numberOfWorkerThreadsInConfig)
        {
            int workerThreadsInLicenseFile = Configure.Instance.Builder.Build<LicenseManager>().CurrentLicense.AllowedNumberOfThreads;

            return Math.Min(workerThreadsInLicenseFile, numberOfWorkerThreadsInConfig);
        }
    }
}
