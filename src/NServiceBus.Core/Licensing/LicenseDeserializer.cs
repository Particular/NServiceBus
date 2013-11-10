namespace NServiceBus.Licensing
{
    using System;
    using System.Linq;
    using System.Xml;

    static class LicenseDeserializer
    {
        public static License GetBasicLicense()
        {
            return new License
            {
                ExpirationDate = DateTime.MinValue,
                AllowedNumberOfWorkerNodes = MinNumberOfWorkerNodes,
                AllowedNumberOfThreads = MinNumberOfWorkerThreads,
                MaxThroughputPerSecond = MinMessagePerSecondThroughput
            };

        }

        public static License GetMaxLicense(DateTime expiry)
        {
            return new License
            {
                ExpirationDate = expiry,
                AllowedNumberOfWorkerNodes = MaxWorkerNodes,
                MaxThroughputPerSecond = MaxThroughputPerSecond,
                AllowedNumberOfThreads = MaxOfWorkerThreads,
            };
        }
        public static License Deserialize(string licenseText)
        {
            //todo: null checks and throw useful exceptions
            var license = new License ();
            var doc = new XmlDocument();
            doc.LoadXml(licenseText);
            var id = doc.SelectSingleNode("/license/@id");

            license.UserId = new Guid(id.Value);

            var date = doc.SelectSingleNode("/license/@expiration");
            license.ExpirationDate = UniversalDateParser.Parse(date.Value.Split('T').First());

            var name = doc.SelectSingleNode("/license/name/text()");
            license.Name = name.Value;

            var licenseVersion = doc.SelectSingleNode("/license/@LicenseVersion");
            license.LicenseVersion = licenseVersion.Value;

            var maxMessageThroughputPerSecond = doc.SelectSingleNode("/license/@MaxMessageThroughputPerSecond").Value;
            if (maxMessageThroughputPerSecond == "Max")
            {
                license.MaxThroughputPerSecond = MaxThroughputPerSecond;
            }
            else
            {
                license.MaxThroughputPerSecond = int.Parse(maxMessageThroughputPerSecond);
            }

            var workerThreads = doc.SelectSingleNode("/license/@WorkerThreads").Value;
            //TODO: if null should this be 1?
            if (workerThreads == "Max")
            {
                license.AllowedNumberOfThreads = MaxOfWorkerThreads;
            }
            else
            {
                license.AllowedNumberOfThreads = int.Parse(workerThreads);
            }

            var allowedNumberOfWorkerNodes = doc.SelectSingleNode("/license/@AllowedNumberOfWorkerNodes").Value;
            if (allowedNumberOfWorkerNodes == "Max")
            {
                license.AllowedNumberOfWorkerNodes = MaxWorkerNodes;   
            }
            else
            {
                license.AllowedNumberOfWorkerNodes = int.Parse(allowedNumberOfWorkerNodes);   
            }

            var upgradeProtectionExpiration = doc.SelectSingleNode("/license/@UpgradeProtectionExpiration");
            //UpgradeProtectionExpiration will not exist for trial licenses
            if (upgradeProtectionExpiration != null)
            {
                license.UpgradeProtectionExpiration = UniversalDateParser.Parse(upgradeProtectionExpiration.Value);
            }

            return license;
        }

        public const int MaxWorkerNodes = int.MaxValue;
        public const int MaxThroughputPerSecond = 0;
        public const int MinNumberOfWorkerNodes = 2;
        public const int MinNumberOfWorkerThreads = 1;
        public const int MinMessagePerSecondThroughput = 1;
        public const int MaxOfWorkerThreads = 1024;
    }
}