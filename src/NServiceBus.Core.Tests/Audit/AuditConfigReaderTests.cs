namespace NServiceBus.Core.Tests.Audit
{
    using NUnit.Framework;
    using Settings;

    public class AuditConfigReaderTests
    {
        [Test]
        public void ShouldUseExplictValueInSettingsIfPresent()
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
    }
}