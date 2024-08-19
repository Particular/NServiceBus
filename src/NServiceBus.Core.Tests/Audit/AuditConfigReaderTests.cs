namespace NServiceBus.Core.Tests.Audit;

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

        settingsHolder.Set(new AuditConfigReader.Result(configuredAddress, null));

        Assert.Multiple(() =>
        {
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out var address), Is.True);
            Assert.That(address, Is.EqualTo(configuredAddress));
        });
    }

    [Test]
    public void ShouldReturnConfiguredExpiration()
    {
        var settingsHolder = new SettingsHolder();
        var configuredExpiration = TimeSpan.FromSeconds(10);

        settingsHolder.Set(new AuditConfigReader.Result("someAddress", configuredExpiration));

        Assert.Multiple(() =>
        {
            Assert.That(settingsHolder.TryGetAuditMessageExpiration(out var expiration), Is.True);
            Assert.That(expiration, Is.EqualTo(configuredExpiration));
        });
    }

    [Test]
    public void ShouldReturnFalseIfNoExpirationIsConfigured()
    {
        Assert.That(new SettingsHolder().TryGetAuditMessageExpiration(out _), Is.False);
    }
}