namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class LicenseDeserializerTests
    {

        [Test]
        [Explicit]
        public void ParseAllTheLicenses()
        {
            //Set an environment variable to a path containing all the licenses to run this test
            var allTheLicensesDir = Environment.GetEnvironmentVariable("NServiceBusLicensesPath");
            foreach (var licensePath in Directory.EnumerateFiles(allTheLicensesDir, "license.xml", SearchOption.AllDirectories))
            {
                var licenseManager = LicenseDeserializer.Deserialize(File.ReadAllText(licensePath));
                Debug.WriteLine(licenseManager.UpgradeProtectionExpiration);
            }
        }

        [Test]
        public void WithAllProperties()
        {
            var license = LicenseDeserializer.Deserialize(ResourceReader.ReadResourceAsString("Licensing.LicenseWithAllProperties.xml"));

            var dateTimeOffset = new DateTime(2014, 2, 6, 0, 0, 0,DateTimeKind.Utc);
            Assert.AreEqual(dateTimeOffset, license.ExpirationDate);
            Assert.AreEqual(LicenseDeserializer.MaxOfWorkerThreads, license.AllowedNumberOfThreads);
            Assert.AreEqual(LicenseDeserializer.MaxWorkerNodes, license.AllowedNumberOfWorkerNodes);
            Assert.AreEqual(LicenseDeserializer.MaxThroughputPerSecond, license.MaxThroughputPerSecond);
            Assert.AreEqual(new DateTime(2013, 11, 6, 0, 0, 0,DateTimeKind.Utc), license.UpgradeProtectionExpiration);
            Assert.AreEqual("4.2",license.LicenseVersion);
            Assert.AreEqual("74a8f2b3-2730-43e9-ab27-fe20d36bcedd", license.UserId.ToString());
            Assert.AreEqual("John Smith", license.Name);
        }

        [Test]
        public void WithNoUpgradeProtection()
        {
            var license = LicenseDeserializer.Deserialize(ResourceReader.ReadResourceAsString("Licensing.LicenseWithNoUpgradeProtection.xml"));
            Assert.IsNull(license.UpgradeProtectionExpiration);
        }

        [Test]
        public void WithNonMaxThresholds()
        {
            var license = LicenseDeserializer.Deserialize(ResourceReader.ReadResourceAsString("Licensing.LicenseWithNonMaxThresholds.xml"));
            Assert.AreEqual(2, license.AllowedNumberOfThreads);
            Assert.AreEqual(2, license.AllowedNumberOfWorkerNodes);
            Assert.AreEqual(2, license.MaxThroughputPerSecond);
        }
    }
}