namespace NServiceBus.Core.Tests.Licensing;

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

        LicenseManager.LogLicenseStatus(LicenseStatus.Valid, logger, new License(), "fake-url");

        Assert.That(logger.Logs, Is.Empty);
    }

    [Test]
    public void WhenSubscriptionLicenseExpired()
    {
        var logger = new TestableLogger();

        LicenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredSubscription, logger, new License(), "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Error));
            Assert.That(logger.Logs[0].message, Is.EqualTo("License expired. Contact us to renew your license: contact@particular.net"));
        }
    }

    [Test]
    public void WhenUpgradeProtectionExpiredForThisRelease()
    {
        var logger = new TestableLogger();

        LicenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredUpgradeProtection, logger, new License(), "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Error));
            Assert.That(logger.Logs[0].message, Is.EqualTo("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, contact us to renew your license: contact@particular.net"));
        }
    }

    [TestCase(false, "Trial license expired. Get your free development license at fake-url")]
    [TestCase(true, "Development license expired. If you’re still in development, renew your license for free at fake-url otherwise email contact@particular.net")]
    public void WhenTrialLicenseExpired(bool isDevLicenseRenewal, string expectedMessage)
    {
        var logger = new TestableLogger();
        var license = new License { IsExtendedTrial = isDevLicenseRenewal };

        LicenseManager.LogLicenseStatus(LicenseStatus.InvalidDueToExpiredTrial, logger, license, "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Error));
            Assert.That(logger.Logs[0].message, Is.EqualTo(expectedMessage));
        }
    }

    [TestCase(3, false, "Trial license expiring in 3 days. Get your free development license at fake-url")]
    [TestCase(1, false, "Trial license expiring in 1 day. Get your free development license at fake-url")]
    [TestCase(0, false, "Trial license expiring today. Get your free development license at fake-url")]
    [TestCase(3, true, "Development license expiring in 3 days. If you’re still in development, renew your license for free at fake-url otherwise email contact@particular.net")]
    [TestCase(1, true, "Development license expiring in 1 day. If you’re still in development, renew your license for free at fake-url otherwise email contact@particular.net")]
    [TestCase(0, true, "Development license expiring today. If you’re still in development, renew your license for free at fake-url otherwise email contact@particular.net")]
    public void WhenTrialLicenseAboutToExpire(int daysRemaining, bool isDevLicenseRenewal, string expectedMessage)
    {
        var logger = new TestableLogger();
        var today = new DateTime(2012, 12, 12);
        var license = new License
        {
            utcDateTimeProvider = () => today,
            ExpirationDate = today.AddDays(daysRemaining),
            IsExtendedTrial = isDevLicenseRenewal
        };

        LicenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringTrial, logger, license, "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Warn));
            Assert.That(logger.Logs[0].message, Is.EqualTo(expectedMessage));
        }
    }

    [TestCase(3, "License expiring in 3 days. Contact us to renew your license: contact@particular.net")]
    [TestCase(1, "License expiring in 1 day. Contact us to renew your license: contact@particular.net")]
    [TestCase(0, "License expiring today. Contact us to renew your license: contact@particular.net")]
    public void WhenSubscriptionAboutToExpire(int daysRemaining, string expectedMessage)
    {
        var logger = new TestableLogger();
        var today = new DateTime(2012, 12, 12);
        var license = new License
        {
            utcDateTimeProvider = () => today,
            ExpirationDate = today.AddDays(daysRemaining)
        };

        LicenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringSubscription, logger, license, "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Warn));
            Assert.That(logger.Logs[0].message, Is.EqualTo(expectedMessage));
        }
    }

    [TestCase(3, "Upgrade protection expiring in 3 days. Contact us to renew your license: contact@particular.net")]
    [TestCase(1, "Upgrade protection expiring in 1 day. Contact us to renew your license: contact@particular.net")]
    [TestCase(0, "Upgrade protection expiring today. Contact us to renew your license: contact@particular.net")]
    public void WhenUpgradeProtectionAboutToExpire(int daysRemaining, string expectedMessage)
    {
        var logger = new TestableLogger();
        var today = new DateTime(2012, 12, 12);
        var license = new License
        {
            utcDateTimeProvider = () => today,
            UpgradeProtectionExpiration = today.AddDays(daysRemaining)
        };

        LicenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiringUpgradeProtection, logger, license, "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Warn));
            Assert.That(logger.Logs[0].message, Is.EqualTo(expectedMessage));
        }
    }

    [Test]
    public void WhenUpgradeProtectionExpiredForFutureVersions()
    {
        var logger = new TestableLogger();
        var today = new DateTime(2012, 12, 12);
        var license = new License
        {
            utcDateTimeProvider = () => today,
            releaseDateProvider = () => today.AddDays(-20),
            UpgradeProtectionExpiration = today.AddDays(-10)
        };

        LicenseManager.LogLicenseStatus(LicenseStatus.ValidWithExpiredUpgradeProtection, logger, license, "fake-url");

        Assert.That(logger.Logs, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.Logs[0].level, Is.EqualTo(LogLevel.Warn));
            Assert.That(logger.Logs[0].message, Is.EqualTo("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, contact us to renew your license: contact@particular.net"));
        }
    }

    class TestableLogger : ILog
    {
        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        public void Debug(string message) => throw new NotImplementedException();

        public void Debug(string message, Exception exception) => throw new NotImplementedException();

        public void DebugFormat(string format, params object[] args) => throw new NotImplementedException();

        public void Info(string message) => throw new NotImplementedException();

        public void Info(string message, Exception exception) => throw new NotImplementedException();

        public void InfoFormat(string format, params object[] args) => throw new NotImplementedException();

        public void Warn(string message) => Log(message, LogLevel.Warn);

        public void Warn(string message, Exception exception) => throw new NotImplementedException();

        public void WarnFormat(string format, params object[] args) => Log(string.Format(format, args), LogLevel.Warn);

        public void Error(string message) => Log(message, LogLevel.Error);

        public void Error(string message, Exception exception) => throw new NotImplementedException();

        public void ErrorFormat(string format, params object[] args) => throw new NotImplementedException();

        public void Fatal(string message) => Log(message, LogLevel.Fatal);

        public void Fatal(string message, Exception exception) => throw new NotImplementedException();

        public void FatalFormat(string format, params object[] args) => throw new NotImplementedException();

        void Log(string message, LogLevel level) => Logs.Add((message, level));

        public List<(string message, LogLevel level)> Logs { get; } = [];
    }
}