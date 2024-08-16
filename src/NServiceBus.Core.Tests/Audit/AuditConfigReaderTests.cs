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

        Assert.That(settingsHolder.TryGetAuditQueueAddress(out var address), Is.True);
        Assert.AreEqual(configuredAddress, address);
    }

    [Test]
    public void ShouldReturnConfiguredExpiration()
    {
        var settingsHolder = new SettingsHolder();
        var configuredExpiration = TimeSpan.FromSeconds(10);

        settingsHolder.Set(new AuditConfigReader.Result("someAddress", configuredExpiration));

        Assert.That(settingsHolder.TryGetAuditMessageExpiration(out var expiration), Is.True);
        Assert.AreEqual(configuredExpiration, expiration);
    }

    [Test]
    public void ShouldReturnFalseIfNoExpirationIsConfigured()
    {
        Assert.That(new SettingsHolder().TryGetAuditMessageExpiration(out _), Is.False);
    }
}