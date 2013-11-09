namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using NServiceBus.Licensing;
    using NUnit.Framework;


    [TestFixture]
    public class LicenseDowngraderTests
    {

        [Test]
        public void Should_downgrade_if_ExpirationDate_is_in_past()
        {
            string reason;
            var expiredLicense = new License
                {
                    ExpirationDate = DateTime.UtcNow.AddDays(-2)
                };
           Assert.IsTrue(LicenseDowngrader.ShouldLicenseDowngrade(expiredLicense, out reason));
           Assert.AreEqual("Your license has expired.", reason);
        }

        [Test]
        public void Should_not_downgrade_if_ExpirationDate_is_in_future()
        {
            string reason;
            var expiredLicense = new License
                {
                    ExpirationDate = DateTime.UtcNow.AddDays(2)
                };
           Assert.IsFalse(LicenseDowngrader.ShouldLicenseDowngrade(expiredLicense, out reason));
           Assert.IsNull(reason);
        }

        [Test]
        public void Should_not_downgrade_if_ExpirationDate_is_in_future_and_UpgradeProtection_is_not_expired()
        {
            string reason;
            var expiredLicense = new License
                {
                    ExpirationDate = DateTime.UtcNow.AddDays(2),
                    UpgradeProtectionExpiration = TimestampReader.GetBuildTimestamp().AddDays(2)
                };
           Assert.IsFalse(LicenseDowngrader.ShouldLicenseDowngrade(expiredLicense, out reason));
           Assert.IsNull(reason);
        }

        [Test]
        public void Should_downgrade_if_ExpirationDate_is_in_future_and_UpgradeProtection_is_expired()
        {
            string reason;
            var expiredLicense = new License
                {
                    ExpirationDate = DateTime.UtcNow.AddDays(2),
                    UpgradeProtectionExpiration = TimestampReader.GetBuildTimestamp().AddDays(-2)
                };
           Assert.IsTrue(LicenseDowngrader.ShouldLicenseDowngrade(expiredLicense, out reason));
           Assert.AreEqual("Your upgrade protection does not cover this version of NServiceBus.", reason);
        }
    }
}