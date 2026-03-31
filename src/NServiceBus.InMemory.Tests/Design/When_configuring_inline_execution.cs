namespace NServiceBus;

using System;
using NUnit.Framework;

[TestFixture]
public class When_configuring_inline_execution
{
    [Test]
    public void Should_keep_inline_execution_disabled_by_default()
    {
        var transport = new InMemoryTransport();

        Assert.That(transport.InlineExecutionSettings.IsEnabled, Is.False);
    }

    [Test]
    public void Should_enable_inline_execution_when_options_are_provided()
    {
        var broker = new InMemoryBroker();
        var transport = new InMemoryTransport(broker, new InlineExecutionOptions());

        Assert.That(transport.InlineExecutionSettings.IsEnabled, Is.True);
    }

    [Test]
    public void Should_require_inline_execution_options()
    {
#pragma warning disable CS8625
        var exception = Assert.Throws<ArgumentNullException>(() => new InMemoryTransport(new InMemoryBroker(), null));
#pragma warning restore CS8625

        Assert.That(exception!.ParamName, Is.EqualTo("inlineExecutionOptions"));
    }

    [Test]
    public void Should_snapshot_option_values_at_construction()
    {
        var broker = new InMemoryBroker();
        var options = new InlineExecutionOptions
        {
            MoveToErrorQueueOnFailure = false
        };

        var transport = new InMemoryTransport(broker, options);
        options.MoveToErrorQueueOnFailure = true;

        Assert.That(transport.InlineExecutionSettings.MoveToErrorQueueOnFailure, Is.False);
    }
}