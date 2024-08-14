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

        Assert.True(settingsHolder.TryGetAuditQueueAddress(out var address));
        Assert.AreEqual(configuredAddress, address);
    }

    [Test]
    public void ShouldReturnConfiguredExpiration()
    {
        var settingsHolder = new SettingsHolder();
        var configuredExpiration = TimeSpan.FromSeconds(10);

        settingsHolder.Set(new AuditConfigReader.Result("someAddress", configuredExpiration));

        Assert.True(settingsHolder.TryGetAuditMessageExpiration(out var expiration));
        Assert.AreEqual(configuredExpiration, expiration);
    }

    [Test]
    public void ShouldReturnFalseIfNoExpirationIsConfigured()
    {
        Assert.That(new SettingsHolder().TryGetAuditMessageExpiration(out _), Is.False);
    }
}