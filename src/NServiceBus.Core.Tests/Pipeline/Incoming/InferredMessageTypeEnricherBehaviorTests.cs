namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class InferredMessageTypeEnricherBehaviorTests
{
    [Test]
    public async Task Should_set_enclosed_message_types_to_logical_message_type_full_name_when_header_is_missing()
    {
        var behavior = new InferredMessageTypeEnricherBehavior();
        var context = new TestableIncomingLogicalMessageContext();
        context.UpdateMessageInstance(new TestMessage());

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.EnclosedMessageTypes], Is.EqualTo(typeof(TestMessage).FullName));
    }

    [Test]
    public async Task Should_not_overwrite_existing_enclosed_message_types_header()
    {
        var behavior = new InferredMessageTypeEnricherBehavior();
        var context = new TestableIncomingLogicalMessageContext();
        context.UpdateMessageInstance(new TestMessage());
        context.Headers[Headers.EnclosedMessageTypes] = "existing-message-type";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.EnclosedMessageTypes], Is.EqualTo("existing-message-type"));
    }

    class TestMessage;
}
