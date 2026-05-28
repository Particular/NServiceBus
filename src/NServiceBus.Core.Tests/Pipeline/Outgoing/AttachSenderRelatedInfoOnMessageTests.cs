namespace NServiceBus.Core.Tests.Pipeline.Outgoing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Routing;
using Transport;
using NUnit.Framework;
using Testing;
using System.Threading;
using NServiceBus.DelayedDelivery;
using Extensibility;

[TestFixture]
public class AttachSenderRelatedInfoOnMessageTests
{
    [Test]
    public async Task Should_set_the_time_sent_headerAsync()
    {
        var before = DateTimeOffset.UtcNow;
        var message = await InvokeBehaviorAsync();
        var after = DateTimeOffset.UtcNow;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.TimeSent), Is.True);
            Assert.That(DateTimeOffsetHelper.ToDateTimeOffset(message.Headers[Headers.TimeSent]), Is.InRange(before, after));
        }
    }

    [Test]
    public async Task Should_not_override_the_time_sent_headerAsync()
    {
        var timeSent = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
            {Headers.TimeSent, timeSent}
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.TimeSent), Is.True);
            Assert.That(message.Headers[Headers.TimeSent], Is.EqualTo(timeSent));
        }
    }

    [Test]
    public async Task Should_set_the_nsb_version_headerAsync()
    {
        var message = await InvokeBehaviorAsync();

        Assert.That(message.Headers.ContainsKey(Headers.NServiceBusVersion), Is.True);
    }

    [Test]
    public async Task Should_not_override_nsb_version_headerAsync()
    {
        var nsbVersion = "some-crazy-version-number";
        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
             {Headers.NServiceBusVersion, nsbVersion}
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.NServiceBusVersion), Is.True);
            Assert.That(message.Headers[Headers.NServiceBusVersion], Is.EqualTo(nsbVersion));
        }
    }

    [Test]
    public async Task Should_set_deliver_At_header_when_delay_delivery_with_setAsync()
    {
        var message = await InvokeBehaviorAsync(null, new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
            Assert.That(message.GetStash().Get<string>(Headers.StartNewTrace), Is.EqualTo(bool.TrueString));
        }
    }

    [Test]
    public async Task Should_set_deliver_at_header_when_do_not_deliver_before_is_setAsync()
    {
        var doNotDeliverBefore = DateTimeOffset.UtcNow;
        var message = await InvokeBehaviorAsync(null, new DispatchProperties
        {
            DoNotDeliverBefore = new DoNotDeliverBefore(doNotDeliverBefore)
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
            Assert.That(message.Headers[Headers.DeliverAt], Is.EqualTo(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)));
            Assert.That(message.GetStash().Get<string>(Headers.StartNewTrace), Is.EqualTo(bool.TrueString));
        }
    }

    [Test]
    public async Task Should_not_override_deliver_at_headerAsync()
    {
        var doNotDeliverBefore = DateTimeOffset.UtcNow;
        var message = await InvokeBehaviorAsync(new Dictionary<string, string>
        {
            {Headers.DeliverAt, DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)}
        }, new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey(Headers.DeliverAt), Is.True);
            Assert.That(message.Headers[Headers.DeliverAt], Is.EqualTo(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)));
            Assert.That(message.GetStash().TryGet(Headers.StartNewTrace, out string _), Is.False);
        }
    }

    [Test]
    public async Task Should_prefer_delay_delivery_with_when_both_delay_and_do_not_deliver_before_are_setAsync()
    {
        var delay = TimeSpan.FromSeconds(10);
        var doNotDeliverBefore = DateTimeOffset.UtcNow.AddHours(1);
        var before = DateTimeOffset.UtcNow.Add(delay).Subtract(TimeSpan.FromMilliseconds(50));

        var message = await InvokeBehaviorAsync(null, new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(delay),
            DoNotDeliverBefore = new DoNotDeliverBefore(doNotDeliverBefore)
        });

        var deliverAt = DateTimeOffsetHelper.ToDateTimeOffset(message.Headers[Headers.DeliverAt]);
        var after = DateTimeOffset.UtcNow.Add(delay);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deliverAt, Is.InRange(before, after));
            Assert.That(deliverAt, Is.Not.EqualTo(doNotDeliverBefore));
            Assert.That(message.GetStash().Get<string>(Headers.StartNewTrace), Is.EqualTo(bool.TrueString));
        }
    }

    static async Task<TestBehaviorResult> InvokeBehaviorAsync(Dictionary<string, string> headers = null, DispatchProperties dispatchProperties = null, CancellationToken cancellationToken = default)
    {
        var message = new OutgoingMessage("id", headers ?? [], null);
        var stash = new ContextBag();

        if (dispatchProperties != null)
        {
            stash.Set(dispatchProperties);
        }

        await new AttachSenderRelatedInfoOnMessageBehavior()
            .Invoke(new TestableRoutingContext { Message = message, Extensions = stash, RoutingStrategies = new List<UnicastRoutingStrategy> { new UnicastRoutingStrategy("_") }, CancellationToken = cancellationToken }, _ => Task.CompletedTask);

        return new TestBehaviorResult(message, stash);
    }

    readonly record struct TestBehaviorResult(OutgoingMessage Message, ContextBag Stash)
    {
        public Dictionary<string, string> Headers => Message.Headers;
        public ContextBag GetStash() => Stash;
    }
}
