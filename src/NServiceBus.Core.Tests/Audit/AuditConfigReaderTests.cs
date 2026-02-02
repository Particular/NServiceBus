namespace NServiceBus.Core.Tests.Audit;

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Settings;

public class AuditConfigReaderTests
{
    [Test]
    public void ShouldUseExplicitValueInSettingsIfPresent()
    {
        var settingsHolder = new SettingsHolder();
        string configuredAddress = "myAuditQueue";

        settingsHolder.Set(new AuditConfigReader.Result(configuredAddress));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out string address), Is.True);
            Assert.That(address, Is.EqualTo(configuredAddress));
        }
    }

    [Test]
    public void Defaults_to_disabled_when_no_audit_address_is_configured()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = []
        });

        settingsHolder.SetAuditQueueDefaults();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.Get<AuditConfigReader.Result>().Disabled, Is.True);
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out _), Is.False);
        }
    }

    [Test]
    [TestCase("false")]
    [TestCase("FALSE")]
    [TestCase("False")]
    [TestCase(null)]
    public void Should_not_disable_when_IsDisabled_is_false_or_not_set_and_address_is_available(string auditDisabledValue)
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string>
            {
                { AuditConfigReader.IsDisabledEnvironmentVariableKey, auditDisabledValue },
                { AuditConfigReader.AddressEnvironmentVariableKey, "envAuditQueue" }
            }
        });

        settingsHolder.SetAuditQueueDefaults();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.Get<AuditConfigReader.Result>().Disabled, Is.False);
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out var address), Is.True);
            Assert.That(address, Is.EqualTo("envAuditQueue"));
        }
    }

    [Test]
    [TestCase("true")]
    [TestCase("TRUE")]
    [TestCase("True")]
    public void DisablingTakesPrecedenceOverAddress(string auditDisabledValue)
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string>
            {
                { AuditConfigReader.IsDisabledEnvironmentVariableKey, auditDisabledValue },
                { AuditConfigReader.AddressEnvironmentVariableKey, "envAuditQueue" }
            }
        });

        settingsHolder.SetAuditQueueDefaults();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.Get<AuditConfigReader.Result>().Disabled, Is.True);
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out _), Is.False);
        }
    }

    [Test]
    public void UserOverrideTakesPrecedenceOverDefaultFromEnvironment()
    {
        var settingsHolder = new SettingsHolder();
        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment
        {
            ValueToReturn = new Dictionary<string, string>
            {
                { AuditConfigReader.AddressEnvironmentVariableKey, "envAuditQueue" }
            }
        });

        const string configuredAddress = "someAddress";

        settingsHolder.Set(new AuditConfigReader.Result(configuredAddress));
        settingsHolder.SetAuditQueueDefaults();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.Get<AuditConfigReader.Result>().Disabled, Is.False);
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out var address), Is.True);
            Assert.That(address, Is.EqualTo(configuredAddress));
        }
    }

    [Test]
    public void ShouldAllowSettingAddressViaEnvironment()
    {
        var settingsHolder = new SettingsHolder();
        string configuredAddress = "envAuditQueue";

        settingsHolder.Set<SystemEnvironment>(new FakeEnvironment { ValueToReturn = new Dictionary<string, string> { { AuditConfigReader.AddressEnvironmentVariableKey, configuredAddress } } });

        settingsHolder.SetAuditQueueDefaults();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.Get<AuditConfigReader.Result>().Disabled, Is.False);
            Assert.That(settingsHolder.TryGetAuditQueueAddress(out string address), Is.True);
            Assert.That(address, Is.EqualTo(configuredAddress));
        }
    }

    [Test]
    public void ShouldReturnConfiguredExpiration()
    {
        var settingsHolder = new SettingsHolder();
        var configuredExpiration = TimeSpan.FromSeconds(10);

        settingsHolder.Set(new AuditConfigReader.Result("someAddress", configuredExpiration));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settingsHolder.TryGetAuditMessageExpiration(out TimeSpan expiration), Is.True);
            Assert.That(expiration, Is.EqualTo(configuredExpiration));
        }
    }

    [Test]
    public void ShouldReturnFalseIfNoExpirationIsConfigured() => Assert.That(new SettingsHolder().TryGetAuditMessageExpiration(out _), Is.False);

    class FakeEnvironment : SystemEnvironment
    {
        public Dictionary<string, string> ValueToReturn { get; set; }

        public override string GetEnvironmentVariable(string variable) => ValueToReturn.GetValueOrDefault(variable);
    }
}