namespace NServiceBus.Core.Tests.Transports;

using System;
using NUnit.Framework;
using Transport;

[TestFixture]
public class PushRuntimeSettingsTests
{
    [Test]
    public void Should_default_concurrency_to_num_processors()
    {
        Assert.That(new PushRuntimeSettings().MaxConcurrency, Is.EqualTo(Math.Max(2, Environment.ProcessorCount)));
    }

    [Test]
    public void Should_honor_explicit_concurrency_settings()
    {
        Assert.That(new PushRuntimeSettings(10).MaxConcurrency, Is.EqualTo(10));
    }
}