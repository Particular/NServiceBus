namespace NServiceBus.Core.Tests.Performance.TimeToBeReceived;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class ApplyTimeToBeReceivedBehaviorTests
{
    [Test]
    public async Task Should_set_ttbr_header_and_dispatch_property_when_mapping_exists()
    {
        var mappings = new TimeToBeReceivedMappings(new[] { typeof(MessageWithTtbr) }, TimeToBeReceivedMappings.DefaultConvention, true);
        var behavior = new ApplyTimeToBeReceivedBehavior(mappings);
        var context = new TestableOutgoingLogicalMessageContext();
        context.UpdateMessage(new MessageWithTtbr());
        context.Extensions.Set(new DispatchProperties());

        await behavior.Invoke(context, _ => Task.CompletedTask);

        var dispatchProperties = context.Extensions.Get<DispatchProperties>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.TimeToBeReceived], Is.EqualTo(TimeSpan.FromMinutes(2).ToString()));
            Assert.That(dispatchProperties.DiscardIfNotReceivedBefore.MaxTime, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }
    }

    [Test]
    public async Task Should_call_next_and_not_set_ttbr_when_mapping_does_not_exist()
    {
        var mappings = new TimeToBeReceivedMappings(Array.Empty<Type>(), TimeToBeReceivedMappings.DefaultConvention, true);
        var behavior = new ApplyTimeToBeReceivedBehavior(mappings);
        var context = new TestableOutgoingLogicalMessageContext();
        context.UpdateMessage(new MessageWithoutTtbr());
        context.Extensions.Set(new DispatchProperties());
        var nextCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(context.Headers.ContainsKey(Headers.TimeToBeReceived), Is.False);
            Assert.That(context.Extensions.Get<DispatchProperties>().DiscardIfNotReceivedBefore, Is.Null);
        }
    }

    [Test]
    public void Should_throw_when_mapping_exists_but_dispatch_properties_are_missing()
    {
        var mappings = new TimeToBeReceivedMappings(new[] { typeof(MessageWithTtbr) }, TimeToBeReceivedMappings.DefaultConvention, true);
        var behavior = new ApplyTimeToBeReceivedBehavior(mappings);
        var context = new TestableOutgoingLogicalMessageContext();
        context.UpdateMessage(new MessageWithTtbr());

        Assert.That(() => behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [TimeToBeReceived("00:02:00")]
    class MessageWithTtbr;

    class MessageWithoutTtbr;
}
