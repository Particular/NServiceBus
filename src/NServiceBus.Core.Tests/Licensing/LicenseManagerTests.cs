namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NUnit.Framework;
    using Particular.Licensing;

    [TestFixture]
    class LicenseManagerTests
    {
        [Test]
        public void ShouldLogNoStatusMessageWhenLicenseIsValid()
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();

            licenseManager.LogLicenseStatus(LicenseStatus.Valid, logger, new License());

            Assert.AreEqual(0, logger.Logs.Count);
        }

        [Test]
        public void WhenSubscriptionLicenseExpired()
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();

            licenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredSubscription, logger, new License());

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Error, logger.Logs[0].level);
            Assert.AreEqual("Platform license expired. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expired", logger.Logs[0].message);
        }

        [Test]
        public void WhenUpgradeProtectionExpiredForThisRelease()
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();

            licenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredUpgradeProtection, logger, new License());

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Error, logger.Logs[0].level);
            Assert.AreEqual("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expired", logger.Logs[0].message);
        }

        [Test]
        public void WhenTrialLicenseExpired()
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();

            licenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredTrial, logger, new License());

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Error, logger.Logs[0].level);
            Assert.AreEqual("Trial license expired. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expired", logger.Logs[0].message);
        }

        [TestCase(3, "Trial license expiring in 3 days. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expiring")]
        [TestCase(1, "Trial license expiring in 1 day. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expiring")]
        [TestCase(0, "Trial license expiring today. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expiring")]
        public void WhenTrialLicenseAboutToExpire(int daysRemaining, string expectedMessage)
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();
            var today = new DateTime(2012, 12, 12);
            var license = new License
            {
                utcDateTimeProvider = () => today,
                ExpirationDate = today.AddDays(daysRemaining)
            };

            licenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringTrial, logger, license);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Warn, logger.Logs[0].level);
            Assert.AreEqual(expectedMessage, logger.Logs[0].message);
        }

        [TestCase(3, "Platform license expiring in 3 days. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expiring")]
        [TestCase(1, "Platform license expiring in 1 day. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expiring")]
        [TestCase(0, "Platform license expiring today. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expiring")]
        public void WhenSubscriptionAboutToExpire(int daysRemaining, string expectedMessage)
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();
            var today = new DateTime(2012, 12, 12);
            var license = new License
            {
                utcDateTimeProvider = () => today,
                ExpirationDate = today.AddDays(daysRemaining)
            };

            licenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringSubscription, logger, license);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Warn, logger.Logs[0].level);
            Assert.AreEqual(expectedMessage, logger.Logs[0].message);
        }

        [TestCase(3, "Upgrade protection expiring in 3 days. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expiring")]
        [TestCase(1, "Upgrade protection expiring in 1 day. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expiring")]
        [TestCase(0, "Upgrade protection expiring today. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expiring")]
        public void WhenUpgradeProtectionAboutToExpire(int daysRemaining, string expectedMessage)
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();
            var today = new DateTime(2012, 12, 12);
            var license = new License
            {
                utcDateTimeProvider = () => today,
                UpgradeProtectionExpiration = today.AddDays(daysRemaining)
            };

            licenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringUpgradeProtection, logger, license);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Warn, logger.Logs[0].level);
            Assert.AreEqual(expectedMessage, logger.Logs[0].message);
        }

        [Test]
        public void WhenUpgradeProtectionExpiredForFutureVersions()
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager();
            var today = new DateTime(2012, 12, 12);
            var license = new License
            {
                utcDateTimeProvider = () => today,
                releaseDateProvider = () => today.AddDays(-20),
                UpgradeProtectionExpiration = today.AddDays(-10)
            };

            licenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiredUpgradeProtection, logger, license);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(LogLevel.Warn, logger.Logs[0].level);
            Assert.AreEqual("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expired", logger.Logs[0].message);
        }

        class TestableLogger : ILog
        {
            public bool IsDebugEnabled => true;
            public bool IsInfoEnabled => true;
            public bool IsWarnEnabled => true;
            public bool IsErrorEnabled => true;
            public bool IsFatalEnabled => true;

            public void Debug(string message)
            {
                throw new NotImplementedException();
            }

            public void Debug(string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void DebugFormat(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void Info(string message)
            {
                throw new NotImplementedException();
            }

            public void Info(string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void InfoFormat(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void Warn(string message)
            {
                Log(message, LogLevel.Warn);
            }

            public void Warn(string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void WarnFormat(string format, params object[] args)
            {
                Log(string.Format(format, args), LogLevel.Warn);
            }

            public void Error(string message)
            {
                Log(message, LogLevel.Error);
            }

            public void Error(string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void ErrorFormat(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void Fatal(string message)
            {
                Log(message, LogLevel.Fatal);
            }

            public void Fatal(string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void FatalFormat(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            void Log(string message, LogLevel level)
            {
                Logs.Add((message, level));
            }

            public List<(string message, LogLevel level)> Logs { get; } = new List<(string, LogLevel)>();
        }
    }
}