namespace NServiceBus.Core.Tests.Host;

using System;
using NUnit.Framework;

[TestFixture]
public class AppContextSwitchesTests
{
    [Test]
    public void Strict_registered_only_message_metadata_defaults_to_false_when_switch_unset()
    {
        AppContextSwitches.ResetStrictRegisteredOnlyMessageMetadata();

        Assert.That(AppContextSwitches.IsStrictRegisteredOnlyMessageMetadataEnabled, Is.False);
    }

    [Test]
    public void Strict_registered_only_message_metadata_is_enabled_when_switch_is_set()
    {
        using (AppContextSwitchHelper.Enable(AppContextSwitches.StrictRegisteredOnlyMessageMetadataSwitchName))
        {
            Assert.That(AppContextSwitches.IsStrictRegisteredOnlyMessageMetadataEnabled, Is.True);
        }
    }

    [Test]
    public void Strict_registered_only_message_metadata_is_disabled_when_switch_is_set_to_false()
    {
        using (AppContextSwitchHelper.Disable(AppContextSwitches.StrictRegisteredOnlyMessageMetadataSwitchName))
        {
            Assert.That(AppContextSwitches.IsStrictRegisteredOnlyMessageMetadataEnabled, Is.False);
        }
    }

    sealed class AppContextSwitchHelper : IDisposable
    {
        readonly string switchName;

        public static AppContextSwitchHelper Enable(string switchName) => new(switchName, true);

        public static AppContextSwitchHelper Disable(string switchName) => new(switchName, false);

        AppContextSwitchHelper(string switchName, bool value)
        {
            this.switchName = switchName;
            AppContext.SetSwitch(switchName, value);
            AppContextSwitches.ResetStrictRegisteredOnlyMessageMetadata();
        }

        public void Dispose()
        {
            AppContext.SetSwitch(switchName, false);
            AppContextSwitches.ResetStrictRegisteredOnlyMessageMetadata();
        }
    }
}
