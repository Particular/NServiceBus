namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using Microsoft.Win32;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class TrialLicenseReaderTests
    {

        [Test]
        public void When_no_sub_key_exists_one_is_created()
        {
            var subKeyPath = String.Format(@"SOFTWARE\NServiceBus\{0}", NServiceBusVersion.MajorAndMinor);
            Registry.CurrentUser.DeleteSubKey(subKeyPath);
            var expirationFromRegistry = TrialLicenseReader.GetTrialExpirationFromRegistry();
            Assert.AreEqual(DateTime.UtcNow.AddDays(TrialLicenseReader.TRIAL_DAYS).Date, expirationFromRegistry.Date);
            Assert.IsNotNull(Registry.CurrentUser.OpenSubKey(subKeyPath));
        }

        [Test]
        public void When_sub_key_exists_one_is_created_the_value_is_read_from_it()
        {
            var subKeyPath = String.Format(@"SOFTWARE\NServiceBus\{0}", NServiceBusVersion.MajorAndMinor);
            //once to create key
            TrialLicenseReader.GetTrialExpirationFromRegistry();
            //again to read from to create key
            var expirationFromRegistry = TrialLicenseReader.GetTrialExpirationFromRegistry();
            Assert.AreEqual(DateTime.UtcNow.AddDays(TrialLicenseReader.TRIAL_DAYS).Date, expirationFromRegistry.Date);
            Assert.IsNotNull(Registry.CurrentUser.OpenSubKey(subKeyPath));
        }
    }
}