namespace NServiceBus.Core.Tests.Audit
{
    using System;
    using NUnit.Framework;
    using Settings;

    public class AuditConfigReaderTests
    {
        [Test]
        public void ShouldUseExplicitValueInSettingsIfPresent()
        {
            var settingsHolder = new SettingsHolder();
            var configuredAddress = "myAuditQueue";

            settingsHolder.Set(new AuditConfigReader.Result
            {
                Address = configuredAddress
            });

            Assert.True(AuditConfigReader.TryGetAuditQueueAddress(settingsHolder, out var address));
            Assert.AreEqual(configuredAddress, address);
        }

        [Test]
        public void ShouldReturnConfiguredExpiration()
        {
            var settingsHolder = new SettingsHolder();
            var configuredExpiration = TimeSpan.FromSeconds(10);

            settingsHolder.Set(new AuditConfigReader.Result
            {
                TimeToBeReceived = configuredExpiration
            });

            Assert.True(AuditConfigReader.TryGetAuditMessageExpiration(settingsHolder, out var expiration));
            Assert.AreEqual(configuredExpiration, expiration);
        }

        [Test]
        public void ShouldReturnFalseIfNoExpirationIsConfigured()
        {
            Assert.False(AuditConfigReader.TryGetAuditMessageExpiration(new SettingsHolder(), out _));
        }
    }
}