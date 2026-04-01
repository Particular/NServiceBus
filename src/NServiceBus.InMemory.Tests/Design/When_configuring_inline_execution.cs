namespace NServiceBus;

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
    public void Should_enable_inline_execution_when_inline_execution_options_are_provided()
    {
        var broker = new InMemoryBroker();
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker) { InlineExecution = new() });

        Assert.That(transport.InlineExecutionSettings.IsEnabled, Is.True);
    }

    [Test]
    public void Should_snapshot_option_values_at_construction()
    {
        var broker = new InMemoryBroker();
        var options = new InMemoryTransportOptions(broker)
        {
            InlineExecution = new()
            {
                MoveToErrorQueueOnFailure = false
            }
        };

        var transport = new InMemoryTransport(options);
        options.InlineExecution.MoveToErrorQueueOnFailure = true;

        Assert.That(transport.InlineExecutionSettings.MoveToErrorQueueOnFailure, Is.False);
    }
}