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
            string address;
            var settingsHolder = new SettingsHolder();

            settingsHolder.Set<AuditConfigReader.Result>(new AuditConfigReader.Result
            {
                Address = "myAuditQueue"
            });

            Assert.True(AuditConfigReader.TryGetAuditQueueAddress(settingsHolder, out address));
            Assert.AreEqual("myAuditQueue", address);
        }
        
        [Test]
        public void ShouldReturnConfiguredExpiration()
        {
            var settingsHolder = new SettingsHolder();

            var configuredExpiration = TimeSpan.FromSeconds(10);
            settingsHolder.Set<AuditConfigReader.Result>(new AuditConfigReader.Result
            {
                TimeToBeReceived = configuredExpiration
            });

            TimeSpan expiration;
            Assert.True(AuditConfigReader.TryGetAuditMessageExpiration(settingsHolder, out expiration));
            Assert.AreEqual(configuredExpiration, expiration);
        }

        [Test]
        public void ShouldReturnFalseIfNoExpirationIsConfigured()
        {
            Assert.False(AuditConfigReader.TryGetAuditMessageExpiration(new SettingsHolder(), out TimeSpan _));
        }
    }
}