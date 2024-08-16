namespace NServiceBus.Core.Tests.Recoverability;

using System;
using NUnit.Framework;
using Settings;

[TestFixture]
public class RecoverabilitySettingsExtensionsTests
{
    [Test]
    public void When_no_unrecoverable_exception_present_should_add_exception_type()
    {
        var settings = new SettingsHolder();
        settings.AddUnrecoverableException(typeof(Exception));

        var result = settings.UnrecoverableExceptions();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Contains(typeof(Exception)), Is.True);
    }

    [Test]
    public void When_unrecoverable_exception_present_should_add_exception_type()
    {
        var settings = new SettingsHolder();
        settings.AddUnrecoverableException(typeof(Exception));
        settings.AddUnrecoverableException(typeof(InvalidOperationException));

        var result = settings.UnrecoverableExceptions();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Contains(typeof(InvalidOperationException)), Is.True);
    }

    [Test]
    public void When_adding_two_times_the_same_type_should_deduplicate()
    {
        var settings = new SettingsHolder();
        settings.AddUnrecoverableException(typeof(Exception));
        settings.AddUnrecoverableException(typeof(Exception));

        var result = settings.UnrecoverableExceptions();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Contains(typeof(Exception)), Is.True);
    }
}