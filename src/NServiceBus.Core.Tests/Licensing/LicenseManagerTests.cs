namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NServiceBus.Logging;
    using NUnit.Framework;
    using Particular.Licensing;

    [TestFixture]
    public class LicenseManagerTests
    {
        [TestCase("2012-12-12", "2012-12-13")]
        [TestCase("2012-12-12", "2012-12-14")]
        [TestCase("2012-12-12", "2012-12-15")]
        public void ShouldWarnAboutExpiringTrialLicense(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your trial or purchase a license to continue using the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Warn, logger.Logs.Single().Item2);
        }

        [TestCase("2012-12-12", "2012-12-16")]
        [TestCase("2012-12-12", "2012-12-17")]
        public void ShouldNotWarnAboutExpiringTrialLicenseWhenMoreThanThreeDaysAway(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(0, logger.Logs.Count);
        }

        [Test]
        public void ShouldLogErrorAboutExpiredTrialLicense()
        {
            var licenseManager = new LicenseManager(() => new DateTime(2012, 12, 12));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = new DateTime(2010, 10, 10),
                LicenseType = "trial"
            };

            licenseManager.LogExpiredLicenseError(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your trial or purchase a license to continue using the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Error, logger.Logs.Single().Item2);
        }

        [TestCase("2012-12-12", "2012-12-13")]
        [TestCase("2012-12-12", "2012-12-14")]
        [TestCase("2012-12-12", "2012-12-15")]
        public void ShouldWarnAboutExpiringSubscriptionLicense(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your license to continue using the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Warn, logger.Logs.Single().Item2);
        }

        [TestCase("2012-12-12", "2012-12-16")]
        [TestCase("2012-12-12", "2012-12-17")]
        public void ShouldNotWarnAboutExpiringLicenseWhenMoreThanThreeDaysAway(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(0, logger.Logs.Count);
        }

        [Test]
        public void ShouldLogErrorAboutExpiredLicense()
        {
            var licenseManager = new LicenseManager(() => new DateTime(2012, 12, 12));
            var logger = new TestableLogger();
            var license = new License
            {
                ExpirationDate = new DateTime(2010, 10, 10),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiredLicenseError(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your license to continue using the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Error, logger.Logs.Single().Item2);
        }

        [TestCase("2012-12-12", "2012-12-13")]
        [TestCase("2012-12-12", "2012-12-14")]
        [TestCase("2012-12-12", "2012-12-15")]
        public void ShouldWarnAboutExpiringUpgradeProtection(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                UpgradeProtectionExpiration = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Warn, logger.Logs.Single().Item2);
        }

        [TestCase("2012-12-12", "2012-12-16")]
        [TestCase("2012-12-12", "2012-12-17")]
        public void ShouldNotWarnAboutExpiringUpgradeProtectionWhenMoreThanThreeDaysAway(string currentDate, string expirationDate)
        {
            var licenseManager = new LicenseManager(() => DateTime.ParseExact(currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
            var logger = new TestableLogger();
            var license = new License
            {
                UpgradeProtectionExpiration = DateTime.ParseExact(expirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiringLicenseWarning(license, logger);

            Assert.AreEqual(0, logger.Logs.Count);
        }

        [Test]
        public void ShouldLogErrorAboutExpiredUpgradeProtection()
        {
            var licenseManager = new LicenseManager(() => new DateTime(2012, 12, 12));
            var logger = new TestableLogger();
            var license = new License
            {
                UpgradeProtectionExpiration = new DateTime(2010, 10, 10),
                LicenseType = "not-trial"
            };

            licenseManager.LogExpiredLicenseError(license, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual("Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.", logger.Logs.Single().Item1);
            Assert.AreEqual(LogLevel.Error, logger.Logs.Single().Item2);
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
                Log(message, LogLevel.Debug);
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
                Log(message, LogLevel.Info);
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
                throw new NotImplementedException();
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
                Logs.Add(new Tuple<string, LogLevel>(message, level));
            }

            public List<Tuple<string, LogLevel>> Logs { get; } = new List<Tuple<string, LogLevel>>();
        }
    }
}