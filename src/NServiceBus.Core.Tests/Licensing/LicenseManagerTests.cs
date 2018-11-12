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
            var licenseManager = new LicenseManager(() => DateTime.UtcNow);

            licenseManager.LogLicenseStatus(LicenseStatus.Valid, logger);

            Assert.AreEqual(0, logger.Logs.Count);
        }

        [TestCase(LicenseStatus.InvalidDueExpiredTrial, LogLevel.Error, "Trial license expired. Please extend your trial or purchase a license to continue using the Particular Service Platform.")]
        [TestCase(LicenseStatus.InvalidDueExpiredSubscription, LogLevel.Error, "Platform license expired. Please extend your license to continue using the Particular Service Platform.")]
        [TestCase(LicenseStatus.InvalidDueExpiredUpgradeProtection, LogLevel.Error, "Upgrade protection expired. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.")]
        [TestCase(LicenseStatus.ValidWithExpiringTrial, LogLevel.Warn, "Trial license expiring soon. Please extend your trial or purchase a license to continue using the Particular Service Platform.")]
        [TestCase(LicenseStatus.ValidWithExpiringSubscription, LogLevel.Warn, "Platform license expiring soon. Please extend your license to continue using the Particular Service Platform.")]
        [TestCase(LicenseStatus.ValidWithExpiringUpgradeProtection, LogLevel.Warn, "Upgrade protection expiring soon. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.")]
        [TestCase(LicenseStatus.ValidWithExpiredUpgradeProtection, LogLevel.Warn, "Upgrade protection expired. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.")]
        public void ShouldLogLicenseStatus(object status, LogLevel logLevel, string expectedMessage)
        {
            var logger = new TestableLogger();
            var licenseManager = new LicenseManager(() => DateTime.UtcNow);

            licenseManager.LogLicenseStatus((LicenseStatus)status, logger);

            Assert.AreEqual(1, logger.Logs.Count);
            Assert.AreEqual(logLevel, logger.Logs[0].level);
            Assert.AreEqual(expectedMessage, logger.Logs[0].message);
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
                Logs.Add((message, level));
            }

            public List<(string message, LogLevel level)> Logs { get; } = new List<(string, LogLevel)>();
        }
    }
}