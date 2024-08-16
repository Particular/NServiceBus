namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System.Threading.Tasks;
using NServiceBus;
using NUnit.Framework;
using Testing;

[TestFixture]
public class MessageTypeEnricherTest
{
    [Test]
    public async Task When_processing_message_without_enclosed_message_type_header_it_is_addedAsync()
    {
        var behavior = new InferredMessageTypeEnricherBehavior();
        var context = new TestableIncomingLogicalMessageContext();

        Assert.That(context.Headers.ContainsKey(Headers.EnclosedMessageTypes), Is.False);

        await behavior.Invoke(context, messageContext => Task.CompletedTask);

        Assert.That(context.Headers.ContainsKey(Headers.EnclosedMessageTypes), Is.True);
        Assert.That(typeof(object).FullName, Is.EqualTo(context.Headers[Headers.EnclosedMessageTypes]));
    }

    [Test]
    public async Task When_processing_message_with_enclosed_message_type_header_it_is_not_changedAsync()
    {
        var mutator = new InferredMessageTypeEnricherBehavior();
        var context = new TestableIncomingLogicalMessageContext();
        context.Headers.Add(Headers.EnclosedMessageTypes, typeof(string).FullName);

        await mutator.Invoke(context, messageContext => Task.CompletedTask);

        Assert.That(context.Headers.ContainsKey(Headers.EnclosedMessageTypes), Is.True);
        Assert.That(typeof(string).FullName, Is.EqualTo(context.Headers[Headers.EnclosedMessageTypes]));

    }
}